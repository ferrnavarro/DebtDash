using DebtDash.Web.Domain.Models;
using DebtDash.Web.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DebtDash.Web.UnitTests.Domain;

public class ProjectionServiceTests
{
    private readonly ProjectionService _sut = new(NullLogger<ProjectionService>.Instance);

    private static LoanProfile MakeLoan(decimal principal = 200000m, decimal rate = 5.5m,
        int term = 360, string start = "2024-01-15") => new()
    {
        Id = Guid.NewGuid(),
        InitialPrincipal = principal,
        AnnualRate = rate,
        TermMonths = term,
        StartDate = DateOnly.Parse(start),
        CurrencyCode = "USD",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static PaymentLogEntry MakePayment(Guid loanId, string date,
        decimal principal, decimal remainingBalance, decimal interest = 0m, int days = 30) => new()
    {
        Id = Guid.NewGuid(),
        LoanProfileId = loanId,
        PaymentDate = DateOnly.Parse(date),
        TotalPaid = principal + interest,
        PrincipalPaid = principal,
        InterestPaid = interest,
        FeesPaid = 0m,
        DaysSincePreviousPayment = days,
        RemainingBalanceAfterPayment = remainingBalance,
        CalculatedRealRate = 5.5m,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public void No_payments_returns_baseline_projection()
    {
        var loan = MakeLoan();
        var result = _sut.CalculateProjection(loan, []);

        result.RemainingMonthsEstimate.Should().Be(360m);
        result.DeltaMonthsVsBaseline.Should().Be(0m);
        result.PredictedEndDate.Should().Be(loan.StartDate.AddMonths(360));
    }

    [Fact]
    public void Single_payment_uses_payment_as_velocity()
    {
        var loan = MakeLoan(principal: 100000m, term: 120);
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-15", 1000m, 99000m)
        };

        var result = _sut.CalculateProjection(loan, payments);

        // Single payment: velocity = 1000 (principal paid)
        // Remaining = 99000 / 1000 = 99 months
        result.RemainingMonthsEstimate.Should().Be(99m);
        result.PrincipalVelocity.Should().Be(1000m);
    }

    [Fact]
    public void Multiple_payments_calculates_average_velocity()
    {
        var loan = MakeLoan(principal: 100000m, term: 120, start: "2024-01-01");

        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-01", 1000m, 99000m, days: 31),
            MakePayment(loan.Id, "2024-03-01", 1200m, 97800m, days: 29),
            MakePayment(loan.Id, "2024-04-01", 800m, 97000m, days: 31),
        };

        var result = _sut.CalculateProjection(loan, payments);

        // Total principal = 3000, span = Feb1 to Apr1 = 60 days ≈ 1.97 months
        // Velocity = 3000 / 1.97 ≈ 1522.34
        result.PrincipalVelocity.Should().BeGreaterThan(0);
        result.RemainingMonthsEstimate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Faster_payments_show_negative_delta()
    {
        var loan = MakeLoan(principal: 100000m, term: 120, start: "2024-01-01");

        // Pay $2000 principal per month, much faster than baseline ($833/mo)
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-01", 2000m, 98000m, days: 31),
            MakePayment(loan.Id, "2024-03-01", 2000m, 96000m, days: 29),
            MakePayment(loan.Id, "2024-04-01", 2000m, 94000m, days: 31),
        };

        var result = _sut.CalculateProjection(loan, payments);

        // Paying faster than baseline should result in negative delta (finishing early)
        result.DeltaMonthsVsBaseline.Should().BeLessThan(0);
    }

    [Fact]
    public void Projection_has_valid_predicted_end_date()
    {
        var loan = MakeLoan(principal: 100000m, term: 120, start: "2024-01-01");
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-01", 1000m, 99000m),
        };

        var result = _sut.CalculateProjection(loan, payments);

        result.PredictedEndDate.Should().BeAfter(DateOnly.Parse("2024-02-01"));
    }
}
