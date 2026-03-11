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

public class ImportConfirmRequestValidator : AbstractValidator<ImportConfirmRequest>
{
    public ImportConfirmRequestValidator()
    {
        RuleFor(x => x.Rows)
            .NotEmpty()
            .WithMessage("Import rows must not be empty.");
        RuleFor(x => x.Rows)
            .Must(r => r.Count <= 500)
            .When(x => x.Rows is { Count: > 0 })
            .WithMessage("Cannot import more than 500 rows at once.");
    }
}

public class PaymentScheduleRequestValidator : AbstractValidator<PaymentScheduleRequest>
{
    public PaymentScheduleRequestValidator()
    {
        RuleFor(x => x.PayoffDate)
            .Must(d =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var months = (d.Year - today.Year) * 12 + (d.Month - today.Month);
                return months >= 1;
            })
            .WithMessage("Payoff date must be at least one full month in the future.");

        RuleFor(x => x.FeeAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.FeeAmount.HasValue)
            .WithMessage("Fee amount cannot be negative.");
    }
}
