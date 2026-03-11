using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Services;
using FluentValidation;

namespace DebtDash.Web.Api;

public static class CalculatorEndpoints
{
    public static RouteGroupBuilder MapCalculatorEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/default-fee", async (IPaymentScheduleCalculatorService service) =>
        {
            var fee = await service.GetDefaultFeeAsync();
            return Results.Ok(fee);
        });

        group.MapPost("/schedule", async (
            PaymentScheduleRequest request,
            IValidator<PaymentScheduleRequest> validator,
            IPaymentScheduleCalculatorService service) =>
        {
            return await ValidationExtensions.ValidateAndProcess(request, validator, async () =>
            {
                var schedule = await service.CalculateScheduleAsync(request);
                return Results.Ok(schedule);
            });
        });

        return group;
    }
}
