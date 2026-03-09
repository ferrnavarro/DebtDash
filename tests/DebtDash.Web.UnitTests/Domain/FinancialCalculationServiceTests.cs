using DebtDash.Web.Domain.Calculations;
using FluentAssertions;

namespace DebtDash.Web.UnitTests.Domain;

public class FinancialCalculationServiceTests
{
    private readonly FinancialCalculationService _sut = new();

    [Theory]
    [InlineData(100000, 6.0, 30, 493.15)] // 100k * 6% * 30/365 = 493.15
    [InlineData(200000, 5.5, 31, 934.25)] // 200k * 5.5% * 31/365 = 934.25
    [InlineData(50000, 4.0, 28, 153.42)]  // 50k * 4% * 28/365 = 153.42
    public void CalculateExpectedInterest_returns_correct_interest(
        decimal balance, decimal annualRate, int days, decimal expected)
    {
        var result = _sut.CalculateExpectedInterest(balance, annualRate, days);
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateExpectedInterest_returns_zero_for_zero_balance()
    {
        _sut.CalculateExpectedInterest(0m, 5.0m, 30).Should().Be(0m);
    }

    [Fact]
    public void CalculateExpectedInterest_returns_zero_for_zero_rate()
    {
        _sut.CalculateExpectedInterest(100000m, 0m, 30).Should().Be(0m);
    }

    [Fact]
    public void CalculateExpectedInterest_returns_zero_for_zero_days()
    {
        _sut.CalculateExpectedInterest(100000m, 5.0m, 0).Should().Be(0m);
    }

    [Theory]
    [InlineData(500, 100000, 30, 6.083333)] // (500/100000) * (365/30) * 100 = 6.08333..
    [InlineData(450, 100000, 31, 5.298387)] // (450/100000) * (365/31) * 100
    public void CalculateRealAnnualRate_returns_correct_rate(
        decimal interestPaid, decimal balance, int days, decimal expected)
    {
        var result = _sut.CalculateRealAnnualRate(interestPaid, balance, days);
        result.Should().Be(expected);
    }

    [Fact]
    public void CalculateRealAnnualRate_returns_zero_for_zero_balance()
    {
        _sut.CalculateRealAnnualRate(500m, 0m, 30).Should().Be(0m);
    }

    [Fact]
    public void CalculateRealAnnualRate_returns_zero_for_zero_days()
    {
        _sut.CalculateRealAnnualRate(500m, 100000m, 0).Should().Be(0m);
    }

    [Fact]
    public void CalculateDaysElapsed_returns_correct_day_count()
    {
        var prev = new DateOnly(2024, 1, 15);
        var curr = new DateOnly(2024, 2, 15);
        _sut.CalculateDaysElapsed(prev, curr).Should().Be(31);
    }

    [Fact]
    public void CalculateDaysElapsed_handles_same_date()
    {
        var date = new DateOnly(2024, 6, 1);
        _sut.CalculateDaysElapsed(date, date).Should().Be(0);
    }

    [Theory]
    [InlineData(100000, 5000, 95000)]
    [InlineData(1000, 1000, 0)]
    [InlineData(500, 600, 0)] // Cannot go negative
    public void CalculateRemainingBalance_returns_correct_balance(
        decimal previousBalance, decimal principalPaid, decimal expected)
    {
        _sut.CalculateRemainingBalance(previousBalance, principalPaid).Should().Be(expected);
    }
}
