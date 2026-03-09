using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Models;
using DebtDash.Web.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DebtDash.Web.UnitTests.Domain;

public class DashboardAggregationServiceTests
{
    private readonly DashboardAggregationService _sut = new(NullLogger<DashboardAggregationService>.Instance);

    private static LoanProfile MakeLoan() => new()
    {
        Id = Guid.NewGuid(),
        InitialPrincipal = 200000m,
        AnnualRate = 5.5m,
        TermMonths = 360,
        StartDate = DateOnly.Parse("2024-01-15"),
        CurrencyCode = "USD",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static PaymentLogEntry MakePayment(Guid loanId, string date,
        decimal principal, decimal interest, decimal remaining, decimal calcRate = 5.5m) => new()
    {
        Id = Guid.NewGuid(),
        LoanProfileId = loanId,
        PaymentDate = DateOnly.Parse(date),
        TotalPaid = principal + interest,
        PrincipalPaid = principal,
        InterestPaid = interest,
        FeesPaid = 0m,
        DaysSincePreviousPayment = 30,
        RemainingBalanceAfterPayment = remaining,
        CalculatedRealRate = calcRate,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public void No_payments_returns_zero_totals()
    {
        var loan = MakeLoan();
        var result = _sut.BuildDashboard(loan, []);

        result.TotalInterestPaid.Should().Be(0m);
        result.TotalCapitalPaid.Should().Be(0m);
        result.AverageRealRateWeighted.Should().Be(0m);
    }

    [Fact]
    public void Calculates_total_interest_and_capital()
    {
        var loan = MakeLoan();
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-15", 1000m, 500m, 199000m),
            MakePayment(loan.Id, "2024-03-15", 1100m, 480m, 197900m),
        };

        var result = _sut.BuildDashboard(loan, payments);

        result.TotalInterestPaid.Should().Be(980m);
        result.TotalCapitalPaid.Should().Be(2100m);
    }

    [Fact]
    public void Builds_trend_series_in_payment_order()
    {
        var loan = MakeLoan();
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-15", 1000m, 500m, 199000m),
            MakePayment(loan.Id, "2024-03-15", 1100m, 480m, 197900m),
        };

        var result = _sut.BuildDashboard(loan, payments);

        result.PrincipalInterestTrendSeries.Should().HaveCount(2);
        result.PrincipalInterestTrendSeries[0].PrincipalPaid.Should().Be(1000m);
        result.PrincipalInterestTrendSeries[1].PrincipalPaid.Should().Be(1100m);
    }

    [Fact]
    public void Builds_countdown_series()
    {
        var loan = MakeLoan();
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-15", 1000m, 500m, 199000m),
            MakePayment(loan.Id, "2024-03-15", 1100m, 480m, 197900m),
        };

        var result = _sut.BuildDashboard(loan, payments);

        result.DebtCountdownSeries.Should().HaveCount(2);
        result.DebtCountdownSeries[0].RemainingBalance.Should().Be(199000m);
        result.DebtCountdownSeries[1].RemainingBalance.Should().Be(197900m);
    }

    [Fact]
    public void Calculates_weighted_average_rate()
    {
        var loan = MakeLoan();
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-15", 1000m, 500m, 199000m, calcRate: 5.0m),
            MakePayment(loan.Id, "2024-03-15", 1100m, 480m, 197900m, calcRate: 6.0m),
        };

        var result = _sut.BuildDashboard(loan, payments);

        result.AverageRealRateWeighted.Should().BeGreaterThan(0);
        // Weighted by balance, so the average should be between 5.0 and 6.0
        result.AverageRealRateWeighted.Should().BeInRange(5.0m, 6.0m);
    }

    [Fact]
    public void Time_remaining_reflects_proportion_of_original_term()
    {
        var loan = MakeLoan(); // 360 months, 200k
        var payments = new List<PaymentLogEntry>
        {
            MakePayment(loan.Id, "2024-02-15", 100000m, 500m, 100000m),
        };

        var result = _sut.BuildDashboard(loan, payments);

        // 50% of original principal remains, so time remaining should be ~180 months
        result.TimeRemainingMonths.Should().Be(180m);
    }
}
