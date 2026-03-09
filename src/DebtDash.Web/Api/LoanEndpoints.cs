using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Services;
using FluentValidation;

namespace DebtDash.Web.Api;

public static class LoanEndpoints
{
    public static RouteGroupBuilder MapLoanEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ILoanProfileService service) =>
        {
            var loan = await service.GetAsync();
            if (loan is null)
                return Results.NotFound();

            return Results.Ok(new LoanProfileResponse(
                loan.Id, loan.InitialPrincipal, loan.AnnualRate,
                loan.TermMonths, loan.StartDate, loan.FixedMonthlyCosts,
                loan.CurrencyCode));
        });

        group.MapPut("/", async (
            LoanProfileUpsertRequest request,
            IValidator<LoanProfileUpsertRequest> validator,
            ILoanProfileService service) =>
        {
            return await ValidationExtensions.ValidateAndProcess(request, validator, async () =>
            {
                var loan = await service.UpsertAsync(request);
                return Results.Ok(new LoanProfileResponse(
                    loan.Id, loan.InitialPrincipal, loan.AnnualRate,
                    loan.TermMonths, loan.StartDate, loan.FixedMonthlyCosts,
                    loan.CurrencyCode));
            });
        });

        return group;
    }
}
