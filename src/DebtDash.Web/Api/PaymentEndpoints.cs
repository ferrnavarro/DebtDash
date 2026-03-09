using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Services;
using FluentValidation;

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
