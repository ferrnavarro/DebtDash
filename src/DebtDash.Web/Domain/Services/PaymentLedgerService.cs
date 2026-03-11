using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Models;
using DebtDash.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Domain.Services;

public interface IPaymentLedgerService
{
    Task<(List<PaymentLogEntry> Items, int TotalCount)> ListAsync(int page, int pageSize);
    Task<PaymentLogEntry> CreateAsync(PaymentUpsertRequest request);
    Task<PaymentLogEntry> UpdateAsync(Guid paymentId, PaymentUpsertRequest request);
    Task DeleteAsync(Guid paymentId);
    Task<ImportConfirmResponse> ImportAsync(List<CsvPaymentRow> rows);
}

public class PaymentLedgerService(
    DebtDashDbContext db,
    IFinancialCalculationService calculator,
    IRateVarianceService varianceService,
    ILogger<PaymentLedgerService> logger) : IPaymentLedgerService
{
    public async Task<(List<PaymentLogEntry> Items, int TotalCount)> ListAsync(int page, int pageSize)
    {
        var query = db.PaymentLogEntries
            .Include(p => p.RateVariance)
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<PaymentLogEntry> CreateAsync(PaymentUpsertRequest request)
    {
        var loan = await db.LoanProfiles.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No loan profile configured. Please configure a loan first.");

        ValidatePaymentComponents(request);

        var entry = new PaymentLogEntry
        {
            Id = Guid.NewGuid(),
            LoanProfileId = loan.Id,
            PaymentDate = request.PaymentDate,
            TotalPaid = request.TotalPaid,
            PrincipalPaid = request.PrincipalPaid,
            InterestPaid = request.InterestPaid,
            FeesPaid = request.FeesPaid,
            ManualRateOverrideEnabled = request.ManualRateOverrideEnabled,
            ManualRateOverride = request.ManualRateOverride,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.PaymentLogEntries.Add(entry);
        await db.SaveChangesAsync();

        await RecalculateFromEntry(loan, entry);

        logger.LogInformation(
            "Payment created: {Id}, date={Date}, total={Total:F2}",
            entry.Id, entry.PaymentDate, entry.TotalPaid);

        return entry;
    }

    public async Task<PaymentLogEntry> UpdateAsync(Guid paymentId, PaymentUpsertRequest request)
    {
        var entry = await db.PaymentLogEntries
            .Include(p => p.RateVariance)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        var loan = await db.LoanProfiles.FirstAsync();
        ValidatePaymentComponents(request);

        entry.PaymentDate = request.PaymentDate;
        entry.TotalPaid = request.TotalPaid;
        entry.PrincipalPaid = request.PrincipalPaid;
        entry.InterestPaid = request.InterestPaid;
        entry.FeesPaid = request.FeesPaid;
        entry.ManualRateOverrideEnabled = request.ManualRateOverrideEnabled;
        entry.ManualRateOverride = request.ManualRateOverride;
        entry.UpdatedAt = DateTime.UtcNow;

        if (entry.RateVariance is not null)
        {
            db.RateVarianceRecords.Remove(entry.RateVariance);
            entry.RateVariance = null;
        }

        await db.SaveChangesAsync();
        await RecalculateAllEntries(loan);

        logger.LogInformation("Payment updated: {Id}", entry.Id);
        return entry;
    }

    public async Task DeleteAsync(Guid paymentId)
    {
        var entry = await db.PaymentLogEntries.FindAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        var loan = await db.LoanProfiles.FirstAsync();
        db.PaymentLogEntries.Remove(entry);
        await db.SaveChangesAsync();
        await RecalculateAllEntries(loan);

        logger.LogInformation("Payment deleted: {Id}", paymentId);
    }

    public async Task<ImportConfirmResponse> ImportAsync(List<CsvPaymentRow> rows)
    {
        var loanIds = rows.Select(r => r.LoanId).Distinct().ToList();

        var existingKeys = await db.PaymentLogEntries
            .Where(p => loanIds.Contains(p.LoanProfileId))
            .Select(p => new { p.LoanProfileId, p.PaymentDate, p.TotalPaid })
            .ToListAsync();

        var existingSet = existingKeys
            .Select(k => (k.LoanProfileId, k.PaymentDate, k.TotalPaid))
            .ToHashSet();

        var imported = new List<PaymentLogEntry>();
        var skipped = new List<SkippedRowDetail>();

        foreach (var row in rows)
        {
            var key = (row.LoanId, row.PaymentDate, row.TotalPaid);
            if (existingSet.Contains(key))
            {
                skipped.Add(new SkippedRowDetail(
                    row.RowIndex,
                    $"Duplicate: a payment with date {row.PaymentDate:yyyy-MM-dd} and total {row.TotalPaid:F2} already exists."));
                continue;
            }

            var entry = new PaymentLogEntry
            {
                Id = Guid.NewGuid(),
                LoanProfileId = row.LoanId,
                PaymentDate = row.PaymentDate,
                TotalPaid = row.TotalPaid,
                PrincipalPaid = row.PrincipalPaid,
                InterestPaid = row.InterestPaid,
                FeesPaid = row.FeesPaid,
                ManualRateOverrideEnabled = false,
                ManualRateOverride = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.PaymentLogEntries.Add(entry);
            imported.Add(entry);
            existingSet.Add(key); // prevent intra-batch duplicates
        }

        if (imported.Count > 0)
        {
            await db.SaveChangesAsync();

            var loan = await db.LoanProfiles.FirstOrDefaultAsync();
            if (loan is not null)
                await RecalculateAllEntries(loan);
        }

        logger.LogInformation(
            "CSV import completed: imported={Imported}, skipped={Skipped}",
            imported.Count, skipped.Count);

        return new ImportConfirmResponse(imported.Count, skipped.Count, skipped);
    }

    private async Task RecalculateFromEntry(LoanProfile loan, PaymentLogEntry entry)
    {
        await RecalculateAllEntries(loan);
    }

    private async Task RecalculateAllEntries(LoanProfile loan)
    {
        var allEntries = await db.PaymentLogEntries
            .Include(p => p.RateVariance)
            .OrderBy(p => p.PaymentDate)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync();

        var previousBalance = loan.InitialPrincipal;
        DateOnly previousDate = loan.StartDate;

        foreach (var entry in allEntries)
        {
            var daysElapsed = calculator.CalculateDaysElapsed(previousDate, entry.PaymentDate);
            if (daysElapsed < 0) daysElapsed = 0;

            entry.DaysSincePreviousPayment = daysElapsed;
            entry.RemainingBalanceAfterPayment = calculator.CalculateRemainingBalance(previousBalance, entry.PrincipalPaid);
            entry.CalculatedRealRate = calculator.CalculateRealAnnualRate(entry.InterestPaid, previousBalance, daysElapsed);

            // Evaluate variance
            if (entry.RateVariance is not null)
            {
                db.RateVarianceRecords.Remove(entry.RateVariance);
            }

            decimal? statedRate = entry.ManualRateOverrideEnabled ? entry.ManualRateOverride : loan.AnnualRate;
            var variance = varianceService.EvaluateVariance(entry.CalculatedRealRate, statedRate, entry.Id);
            entry.RateVariance = variance;
            if (variance is not null)
            {
                db.RateVarianceRecords.Add(variance);
            }

            previousBalance = entry.RemainingBalanceAfterPayment;
            previousDate = entry.PaymentDate;
        }

        logger.LogInformation("Recalculated {Count} payment entries", allEntries.Count);
        await db.SaveChangesAsync();
    }

    private static void ValidatePaymentComponents(PaymentUpsertRequest request)
    {
        var componentSum = request.PrincipalPaid + request.InterestPaid + request.FeesPaid;
        if (Math.Abs(componentSum - request.TotalPaid) > 0.01m)
        {
            throw new InvalidOperationException(
                $"Payment components ({componentSum:F2}) must equal total paid ({request.TotalPaid:F2}).");
        }

        if (request.ManualRateOverrideEnabled && request.ManualRateOverride is null)
        {
            throw new InvalidOperationException(
                "Manual rate override value is required when override is enabled.");
        }
    }
}
