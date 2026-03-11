using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Models;
using DebtDash.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Domain.Services;

public interface IPaymentScheduleCalculatorService
{
    Task<FeeDefaultResponse> GetDefaultFeeAsync();
    Task<PaymentScheduleResponse> CalculateScheduleAsync(PaymentScheduleRequest request);
}

public class PaymentScheduleCalculatorService(
    DebtDashDbContext db,
    IFinancialCalculationService calc,
    ILogger<PaymentScheduleCalculatorService> logger) : IPaymentScheduleCalculatorService
{
    private const decimal RateChangeWarningThresholdPp = 0.5m;

    // ── Pure static helpers — testable without a database ─────────────────────

    /// <summary>
    /// Returns the number of whole calendar months between today and payoffDate.
    /// Returns ≤ 0 if payoffDate is in the same month or earlier.
    /// </summary>
    public static int DeriveRemainingPeriods(DateOnly today, DateOnly payoffDate)
        => (payoffDate.Year - today.Year) * 12 + (payoffDate.Month - today.Month);

    /// <summary>
    /// Outstanding balance = InitialPrincipal minus cumulative PrincipalPaid across all ledger entries.
    /// Returns InitialPrincipal when the ledger is empty.
    /// </summary>
    public static decimal ComputeOutstandingBalance(decimal initialPrincipal, IEnumerable<PaymentLogEntry> payments)
        => initialPrincipal - payments.Sum(p => p.PrincipalPaid);

    /// <summary>
    /// Resolves the interest rate to use for the schedule calculation.
    /// Uses CalculatedRealRate from the most recent ledger entry (source "ledger").
    /// Falls back to LoanProfile.AnnualRate when the ledger is empty (source "baseline").
    /// Sets rateChangeWarning when |ledgerRate − baselineRate| > 0.5 percentage points.
    /// </summary>
    public static RateQuoteContext ResolveRateQuote(decimal loanAnnualRate, PaymentLogEntry? mostRecent)
    {
        if (mostRecent is null)
        {
            return new RateQuoteContext(
                AnnualRate: loanAnnualRate,
                Source: RateSource.Baseline,
                ResolvedAt: DateTime.UtcNow,
                IsFallback: true,
                FallbackReason: "No payment ledger entries found; using configured loan rate",
                RateChangedFromBaseline: false,
                RateChangeWarning: false);
        }

        var rate = mostRecent.CalculatedRealRate;
        var delta = Math.Abs(rate - loanAnnualRate);

        return new RateQuoteContext(
            AnnualRate: rate,
            Source: RateSource.Ledger,
            ResolvedAt: DateTime.UtcNow,
            IsFallback: false,
            FallbackReason: null,
            RateChangedFromBaseline: delta > 0m,
            RateChangeWarning: delta > RateChangeWarningThresholdPp);
    }

    /// <summary>
    /// Returns the fee to apply per period.
    /// Uses requestedFee when explicitly provided; otherwise falls back to the most recent ledger FeesPaid.
    /// Returns null when there is no explicit request and the ledger is empty.
    /// </summary>
    public static decimal? ResolveFeeAmount(decimal? requestedFee, PaymentLogEntry? mostRecent)
        => requestedFee ?? mostRecent?.FeesPaid;

    // ── Database-backed methods ────────────────────────────────────────────────

    public async Task<FeeDefaultResponse> GetDefaultFeeAsync()
    {
        var loan = await db.LoanProfiles.FirstOrDefaultAsync();
        if (loan is null)
            throw new KeyNotFoundException("No loan profile configured.");

        var latestEntry = await db.PaymentLogEntries
            .Where(p => p.LoanProfileId == loan.Id)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        return new FeeDefaultResponse(latestEntry?.FeesPaid, latestEntry?.PaymentDate);
    }

    public async Task<PaymentScheduleResponse> CalculateScheduleAsync(PaymentScheduleRequest request)
    {
        var loan = await db.LoanProfiles.FirstOrDefaultAsync();
        if (loan is null)
            throw new KeyNotFoundException("No loan profile configured.");

        var payments = await db.PaymentLogEntries
            .Where(p => p.LoanProfileId == loan.Id)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        var balance = ComputeOutstandingBalance(loan.InitialPrincipal, payments);
        if (balance <= 0m)
            throw new InvalidOperationException(
                "Outstanding loan balance is zero. There is nothing to schedule.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periods = DeriveRemainingPeriods(today, request.PayoffDate);

        var mostRecent = payments.FirstOrDefault();
        var rateQuote = ResolveRateQuote(loan.AnnualRate, mostRecent);
        var feePerPeriod = ResolveFeeAmount(request.FeeAmount, mostRecent) ?? 0m;

        var firstDueMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(1);
        var (monthlyPayment, amortizationPeriods) = calc.CalculateMonthlyAmortizationSchedule(
            balance, rateQuote.AnnualRate, periods, firstDueMonth);

        var entries = amortizationPeriods
            .Select(p => new SchedulePeriodEntry(
                PeriodNumber: p.PeriodNumber,
                DueDate: p.DueDate,
                PrincipalComponent: p.Principal,
                InterestComponent: p.Interest,
                FeeComponent: feePerPeriod,
                TotalPayment: p.Principal + p.Interest + feePerPeriod,
                RemainingBalance: p.RemainingBalance))
            .ToList();

        var summary = new ScheduleSummary(
            TotalPrincipal: entries.Sum(e => e.PrincipalComponent),
            TotalInterest: entries.Sum(e => e.InterestComponent),
            TotalFees: entries.Sum(e => e.FeeComponent),
            TotalAmountPaid: entries.Sum(e => e.TotalPayment),
            PeriodCount: entries.Count);

        logger.LogInformation(
            "Calculated {Periods}-period schedule for loan {LoanId}: balance {Balance:C}, rate {Rate}% (source: {Source})",
            periods, loan.Id, balance, rateQuote.AnnualRate, rateQuote.Source);

        return new PaymentScheduleResponse(
            LoanId: loan.Id,
            OutstandingBalance: balance,
            Periods: periods,
            MonthlyPaymentAmount: monthlyPayment,
            FeeAmountPerPeriod: feePerPeriod,
            TotalMonthlyAmount: monthlyPayment + feePerPeriod,
            RateQuote: rateQuote,
            Entries: entries,
            Summary: summary,
            CalculatedAt: DateTime.UtcNow);
    }
}
