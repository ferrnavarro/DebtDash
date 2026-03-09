using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Api.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DebtDash.Web.UnitTests.Domain;

public class LoanProfileValidationTests
{
    private readonly LoanProfileUpsertRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var request = new LoanProfileUpsertRequest(
            InitialPrincipal: 100000m,
            AnnualRate: 5.5m,
            TermMonths: 360,
            StartDate: new DateOnly(2024, 1, 15),
            FixedMonthlyCosts: 50m,
            CurrencyCode: "USD");

        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Principal_must_be_positive(decimal principal)
    {
        var request = new LoanProfileUpsertRequest(principal, 5.5m, 360, new DateOnly(2024, 1, 1), 0m, "USD");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.InitialPrincipal);
    }

    [Fact]
    public void Negative_rate_is_rejected()
    {
        var request = new LoanProfileUpsertRequest(100000m, -1m, 360, new DateOnly(2024, 1, 1), 0m, "USD");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.AnnualRate);
    }

    [Fact]
    public void Zero_rate_is_allowed()
    {
        var request = new LoanProfileUpsertRequest(100000m, 0m, 360, new DateOnly(2024, 1, 1), 0m, "USD");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.AnnualRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TermMonths_must_be_positive(int term)
    {
        var request = new LoanProfileUpsertRequest(100000m, 5.5m, term, new DateOnly(2024, 1, 1), 0m, "USD");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TermMonths);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    public void CurrencyCode_must_be_3_chars(string code)
    {
        var request = new LoanProfileUpsertRequest(100000m, 5.5m, 360, new DateOnly(2024, 1, 1), 0m, code);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }
}
