using DebtDash.Web.Api.Contracts;
using FluentValidation;

namespace DebtDash.Web.Api.Validators;

public class LoanProfileUpsertRequestValidator : AbstractValidator<LoanProfileUpsertRequest>
{
    public LoanProfileUpsertRequestValidator()
    {
        RuleFor(x => x.InitialPrincipal).GreaterThan(0).WithMessage("Initial principal must be greater than 0.");
        RuleFor(x => x.AnnualRate).GreaterThanOrEqualTo(0).WithMessage("Annual rate cannot be negative.");
        RuleFor(x => x.TermMonths).GreaterThan(0).WithMessage("Term must be at least 1 month.");
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3).WithMessage("Currency code must be a 3-letter code.");
    }
}

public class PaymentUpsertRequestValidator : AbstractValidator<PaymentUpsertRequest>
{
    public PaymentUpsertRequestValidator()
    {
        RuleFor(x => x.TotalPaid).GreaterThan(0).WithMessage("Total paid must be greater than 0.");
        RuleFor(x => x.PrincipalPaid).GreaterThanOrEqualTo(0).WithMessage("Principal paid cannot be negative.");
        RuleFor(x => x.InterestPaid).GreaterThanOrEqualTo(0).WithMessage("Interest paid cannot be negative.");
        RuleFor(x => x.FeesPaid).GreaterThanOrEqualTo(0).WithMessage("Fees paid cannot be negative.");
        RuleFor(x => x.ManualRateOverride)
            .NotNull()
            .When(x => x.ManualRateOverrideEnabled)
            .WithMessage("Manual rate override value is required when override is enabled.");
    }
}
