using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Services;
using DebtDash.Web.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Api;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (int? page, int? pageSize, IPaymentLedgerService service) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 50, 1, 200);
            var (items, total) = await service.ListAsync(p, ps);

            var response = new PaymentListResponse(
                items.Select(MapToResponse).ToList(), p, ps, total);
            return Results.Ok(response);
        });

        group.MapPost("/", async (
            PaymentUpsertRequest request,
            IValidator<PaymentUpsertRequest> validator,
            IPaymentLedgerService service) =>
        {
            return await ValidationExtensions.ValidateAndProcess(request, validator, async () =>
            {
                var entry = await service.CreateAsync(request);
                return Results.Created($"/api/payments/{entry.Id}", MapToResponse(entry));
            });
        });

        group.MapPut("/{paymentId:guid}", async (
            Guid paymentId,
            PaymentUpsertRequest request,
            IValidator<PaymentUpsertRequest> validator,
            IPaymentLedgerService service) =>
        {
            return await ValidationExtensions.ValidateAndProcess(request, validator, async () =>
            {
                var entry = await service.UpdateAsync(paymentId, request);
                return Results.Ok(MapToResponse(entry));
            });
        });

        group.MapDelete("/{paymentId:guid}", async (Guid paymentId, IPaymentLedgerService service) =>
        {
            await service.DeleteAsync(paymentId);
            return Results.NoContent();
        });

        group.MapGet("/import/template", (ICsvImportService csvImport) =>
        {
            var template = csvImport.GenerateTemplate();
            var bytes = System.Text.Encoding.UTF8.GetBytes(template);
            return Results.File(bytes, "text/csv", "payment-import-template.csv");
        });

        group.MapPost("/import/validate", async (
            IFormFile? file,
            ICsvImportService csvImport,
            DebtDashDbContext db) =>
        {
            if (file is null)
                return Results.BadRequest(new { error = "No file was uploaded." });

            var loanIds = await db.LoanProfiles
                .Select(l => l.Id)
                .ToListAsync();

            var (preview, fileError) = await csvImport.ParseAndValidateAsync(file, new HashSet<Guid>(loanIds));

            if (fileError is not null)
                return Results.BadRequest(new { error = fileError });

            return Results.Ok(preview);
        }).DisableAntiforgery();

        group.MapPost("/import/confirm", async (
            ImportConfirmRequest request,
            IValidator<ImportConfirmRequest> validator,
            IPaymentLedgerService service) =>
        {
            return await ValidationExtensions.ValidateAndProcess(request, validator, async () =>
            {
                var result = await service.ImportAsync(request.Rows);
                return Results.Ok(result);
            });
        });

        return group;
    }

    private static PaymentLogEntryResponse MapToResponse(Domain.Models.PaymentLogEntry entry) =>
        new(entry.Id, entry.PaymentDate, entry.TotalPaid,
            entry.PrincipalPaid, entry.InterestPaid, entry.FeesPaid,
            entry.DaysSincePreviousPayment, entry.RemainingBalanceAfterPayment,
            entry.CalculatedRealRate, entry.ManualRateOverrideEnabled,
            entry.ManualRateOverride,
            entry.RateVariance is null ? null : new RateVarianceResponse(
                entry.RateVariance.CalculatedRate,
                entry.RateVariance.StatedOrOverrideRate,
                entry.RateVariance.VarianceAbsolute,
                entry.RateVariance.VarianceBasisPoints,
                entry.RateVariance.IsFlagged));
}
