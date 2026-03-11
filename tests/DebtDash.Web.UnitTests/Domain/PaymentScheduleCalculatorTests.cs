using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Models;
using DebtDash.Web.Domain.Services;
using FluentAssertions;

namespace DebtDash.Web.UnitTests.Domain;

/// <summary>
/// T005 / T014 / T019 / T026: Unit tests for the payment schedule calculator feature.
/// Covers pure mathematical helpers and static logic in
/// FinancialCalculationService and PaymentScheduleCalculatorService
/// without any database or HTTP dependencies.
/// </summary>
public class PaymentScheduleCalculatorTests
{
    private static readonly IFinancialCalculationService CalcService = new FinancialCalculationService();

    // ── T005: PMT formula ─────────────────────────────────────────────────────

    [Fact]
    public void Amortization_typical_24_period_returns_correct_entry_count()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            142350m, 5.5m, 24, new DateOnly(2026, 4, 1));

        periods.Should().HaveCount(24);
    }

    [Fact]
    public void Amortization_monthly_payment_is_positive_and_covers_interest()
    {
        // P=100 000, r=6%/12=0.5%, n=12 → M ≈ 8606.64
        var (monthlyPayment, _) = CalcService.CalculateMonthlyAmortizationSchedule(
            100_000m, 6.0m, 12, new DateOnly(2026, 4, 1));

        monthlyPayment.Should().BeGreaterThan(0m);
        // First period daily interest (April, 30 days) = 100 000 * 6% * 30/365 ≈ 493 — PMT ≈ 8607 must exceed that
        monthlyPayment.Should().BeGreaterThan(493m);
    }

    [Fact]
    public void Amortization_final_period_remaining_balance_is_zero()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            100_000m, 6.0m, 12, new DateOnly(2026, 4, 1));

        var last = periods[^1];
        last.RemainingBalance.Should().BeInRange(-0.01m, 0.01m);
    }

    [Fact]
    public void Amortization_all_periods_have_non_negative_remaining_balance()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            50_000m, 5.0m, 24, new DateOnly(2026, 4, 1));

        periods.Should().AllSatisfy(p => p.RemainingBalance.Should().BeGreaterThanOrEqualTo(0m));
    }

    [Fact]
    public void Amortization_single_period_pays_off_entire_balance()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            10_000m, 12.0m, 1, new DateOnly(2026, 4, 1));

        periods.Should().HaveCount(1);
        periods[0].RemainingBalance.Should().Be(0m);
        periods[0].Principal.Should().Be(10_000m);
    }

    [Fact]
    public void Amortization_zero_rate_divides_balance_equally_with_no_interest()
    {
        var (monthlyPayment, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            12_000m, 0m, 12, new DateOnly(2026, 4, 1));

        monthlyPayment.Should().Be(1_000m);
        periods.Should().HaveCount(12);
        periods[^1].RemainingBalance.Should().Be(0m);
        periods.All(p => p.Interest == 0m).Should().BeTrue();
    }

    [Fact]
    public void Amortization_360_period_schedule_returns_360_entries_with_zero_final_balance()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            300_000m, 5.0m, 360, new DateOnly(2026, 4, 1));

        periods.Should().HaveCount(360);
        periods[^1].RemainingBalance.Should().BeInRange(-0.01m, 0.01m);
    }

    [Fact]
    public void Amortization_due_dates_increment_by_one_month_each_period()
    {
        var firstDue = new DateOnly(2026, 4, 1);
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            50_000m, 5.0m, 3, firstDue);

        periods[0].DueDate.Should().Be(new DateOnly(2026, 4, 1));
        periods[1].DueDate.Should().Be(new DateOnly(2026, 5, 1));
        periods[2].DueDate.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Fact]
    public void Amortization_period_numbers_are_sequential_starting_at_1()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            20_000m, 4.0m, 4, new DateOnly(2026, 4, 1));

        for (var i = 0; i < 4; i++)
            periods[i].PeriodNumber.Should().Be(i + 1);
    }

    // ── T005: Period derivation ───────────────────────────────────────────────

    [Theory]
    [InlineData("2026-03-11", "2026-04-01", 1)]
    [InlineData("2026-03-01", "2028-03-01", 24)]
    [InlineData("2026-03-11", "2056-03-01", 360)]
    [InlineData("2026-03-11", "2026-03-31", 0)] // same month → 0 periods (invalid)
    [InlineData("2026-03-11", "2026-02-01", -1)] // past date → negative
    public void DeriveRemainingPeriods_returns_correct_month_count(
        string today, string payoff, int expected)
    {
        var result = PaymentScheduleCalculatorService.DeriveRemainingPeriods(
            DateOnly.Parse(today), DateOnly.Parse(payoff));

        result.Should().Be(expected);
    }

    // ── T005: Balance computation ─────────────────────────────────────────────

    [Fact]
    public void ComputeOutstandingBalance_subtracts_all_principal_paid()
    {
        var payments = new[]
        {
            new PaymentLogEntry { PrincipalPaid = 1_000m },
            new PaymentLogEntry { PrincipalPaid = 1_500m },
        };
        var balance = PaymentScheduleCalculatorService.ComputeOutstandingBalance(100_000m, payments);

        balance.Should().Be(97_500m);
    }

    [Fact]
    public void ComputeOutstandingBalance_empty_ledger_equals_initial_principal()
    {
        var balance = PaymentScheduleCalculatorService.ComputeOutstandingBalance(200_000m, []);

        balance.Should().Be(200_000m);
    }

    // ── T014: Rate resolution ─────────────────────────────────────────────────

    [Fact]
    public void ResolveRateQuote_with_ledger_entry_uses_calculated_real_rate()
    {
        var entry = new PaymentLogEntry
        {
            CalculatedRealRate = 5.75m,
            PaymentDate = new DateOnly(2026, 2, 15),
        };

        var quote = PaymentScheduleCalculatorService.ResolveRateQuote(5.5m, entry);

        quote.AnnualRate.Should().Be(5.75m);
        quote.Source.Should().Be(RateSource.Ledger);
        quote.IsFallback.Should().BeFalse();
        quote.FallbackReason.Should().BeNull();
    }

    [Fact]
    public void ResolveRateQuote_with_no_entries_falls_back_to_loan_annual_rate()
    {
        var quote = PaymentScheduleCalculatorService.ResolveRateQuote(5.5m, null);

        quote.AnnualRate.Should().Be(5.5m);
        quote.Source.Should().Be(RateSource.Baseline);
        quote.IsFallback.Should().BeTrue();
        quote.FallbackReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ResolveRateQuote_rate_change_warning_true_when_delta_exceeds_50bp()
    {
        var entry = new PaymentLogEntry { CalculatedRealRate = 6.1m };

        var quote = PaymentScheduleCalculatorService.ResolveRateQuote(5.5m, entry);

        quote.RateChangedFromBaseline.Should().BeTrue();
        quote.RateChangeWarning.Should().BeTrue(); // 0.6pp > 0.5pp threshold
    }

    [Fact]
    public void ResolveRateQuote_no_warning_when_delta_is_within_50bp()
    {
        var entry = new PaymentLogEntry { CalculatedRealRate = 5.7m };

        var quote = PaymentScheduleCalculatorService.ResolveRateQuote(5.5m, entry);

        quote.RateChangedFromBaseline.Should().BeTrue();
        quote.RateChangeWarning.Should().BeFalse(); // 0.2pp < 0.5pp threshold
    }

    [Fact]
    public void ResolveRateQuote_rate_changed_false_when_rate_identical_to_baseline()
    {
        var entry = new PaymentLogEntry { CalculatedRealRate = 5.5m };

        var quote = PaymentScheduleCalculatorService.ResolveRateQuote(5.5m, entry);

        quote.RateChangedFromBaseline.Should().BeFalse();
        quote.RateChangeWarning.Should().BeFalse();
    }

    // ── T019: Fee defaulting ──────────────────────────────────────────────────

    [Fact]
    public void ResolveFeeAmount_null_request_uses_ledger_fee()
    {
        var entry = new PaymentLogEntry { FeesPaid = 75m };

        var fee = PaymentScheduleCalculatorService.ResolveFeeAmount(null, entry);

        fee.Should().Be(75m);
    }

    [Fact]
    public void ResolveFeeAmount_explicit_request_overrides_ledger_fee()
    {
        var entry = new PaymentLogEntry { FeesPaid = 75m };

        var fee = PaymentScheduleCalculatorService.ResolveFeeAmount(100m, entry);

        fee.Should().Be(100m);
    }

    [Fact]
    public void ResolveFeeAmount_explicit_zero_overrides_non_zero_ledger_fee()
    {
        var entry = new PaymentLogEntry { FeesPaid = 75m };

        var fee = PaymentScheduleCalculatorService.ResolveFeeAmount(0m, entry);

        fee.Should().Be(0m);
    }

    [Fact]
    public void ResolveFeeAmount_null_request_null_entry_returns_null()
    {
        var fee = PaymentScheduleCalculatorService.ResolveFeeAmount(null, null);

        fee.Should().BeNull();
    }

    [Fact]
    public void ResolveFeeAmount_zero_fee_in_ledger_is_valid_default()
    {
        var entry = new PaymentLogEntry { FeesPaid = 0m };

        var fee = PaymentScheduleCalculatorService.ResolveFeeAmount(null, entry);

        // Zero is a valid default — must not be treated as "no fee"
        fee.Should().NotBeNull();
        fee.Should().Be(0m);
    }

    // ── T026: ScheduleSummary computation ────────────────────────────────────

    [Fact]
    public void Schedule_summary_totals_sum_correctly_across_all_entries()
    {
        const decimal feePerPeriod = 50m;
        var (_, amortPeriods) = CalcService.CalculateMonthlyAmortizationSchedule(
            60_000m, 5.0m, 6, new DateOnly(2026, 4, 1));

        var entries = amortPeriods.Select(p => new SchedulePeriodEntry(
            p.PeriodNumber, p.DueDate, p.Principal, p.Interest,
            feePerPeriod, p.Principal + p.Interest + feePerPeriod,
            p.RemainingBalance)).ToList();

        var totalPrincipal = entries.Sum(e => e.PrincipalComponent);
        var totalInterest = entries.Sum(e => e.InterestComponent);
        var totalFees = entries.Sum(e => e.FeeComponent);
        var totalPaid = entries.Sum(e => e.TotalPayment);

        totalPrincipal.Should().BeApproximately(60_000m, 0.02m,
            "all principal components must sum to the original balance (within rounding)");
        totalFees.Should().Be(6 * feePerPeriod);
        totalPaid.Should().BeApproximately(totalPrincipal + totalInterest + totalFees, 0.02m);
    }

    [Fact]
    public void Schedule_summary_single_period_equals_balance_plus_one_months_interest()
    {
        var (_, periods) = CalcService.CalculateMonthlyAmortizationSchedule(
            12_000m, 6.0m, 1, new DateOnly(2026, 4, 1));

        periods.Should().HaveCount(1);
        var p = periods[0];
        // Single period: principal = 12 000, interest = 12 000 * 6% * 30/365 (April 2026 = 30 days) = 59.18
        p.Principal.Should().Be(12_000m);
        p.Interest.Should().Be(59.18m);
    }
}
