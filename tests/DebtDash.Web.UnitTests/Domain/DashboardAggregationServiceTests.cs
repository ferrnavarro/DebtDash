using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Models;
using DebtDash.Web.Domain.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace DebtDash.Web.UnitTests.Domain;

public class DashboardAggregationServiceTests
{
    private static readonly IFinancialCalculationService CalcService = new FinancialCalculationService();
    private static readonly IComparisonTimelineCalculator TimelineCalc = new ComparisonTimelineCalculator(CalcService);

    private readonly DashboardAggregationService _sut = new(
        NullLogger<DashboardAggregationService>.Instance,
        TimelineCalc);

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

    // ──────────────────────────────────────────────────────────────────────────
    // T013: Comparison summary delta and status tests (US1)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Comparison_returns_empty_state_when_no_payments()
    {
        var loan = ComparisonTestData.StandardLoan();
        var result = _sut.BuildComparisonDashboard(loan, [], DashboardWindowKey.FullHistory);

        result.State.Should().Be(DashboardState.Empty);
        result.Summary.CurrentStatus.Should().Be(ComparisonStatus.InsufficientData);
        result.BalanceSeries.Should().BeEmpty();
        result.CostSeries.Should().BeEmpty();
        result.Milestones.Should().BeEmpty();
    }

    [Fact]
    public void Comparison_returns_limited_data_state_with_single_payment()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SinglePayment(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.State.Should().Be(DashboardState.LimitedData);
        result.Summary.CurrentStatus.Should().Be(ComparisonStatus.InsufficientData);
        result.Summary.ExplanatoryStateMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Comparison_returns_ready_state_with_multiple_payments()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.State.Should().Be(DashboardState.Ready);
        result.Summary.Should().NotBeNull();
        result.AvailableWindows.Should().HaveCount(4);
        result.ActiveWindow.Key.Should().Be(DashboardWindowKey.FullHistory);
    }

    [Fact]
    public void Comparison_status_is_ahead_when_extra_principal_paid()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.MixedPayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.State.Should().Be(DashboardState.Ready);
        result.Summary.CurrentStatus.Should().Be(ComparisonStatus.Ahead);
        result.Summary.CumulativeInterestAvoided.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Comparison_balance_series_is_nonempty_with_sufficient_payments()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.BalanceSeries.Should().NotBeEmpty();
        result.BalanceSeries.Should().AllSatisfy(p =>
        {
            p.ActualRemainingBalance.Should().BeGreaterThanOrEqualTo(0);
            p.BaselineRemainingBalance.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public void Comparison_summary_has_explanatory_message_for_all_states()
    {
        var loan = ComparisonTestData.StandardLoan();

        // Empty state
        var empty = _sut.BuildComparisonDashboard(loan, [], DashboardWindowKey.FullHistory);
        empty.Summary.ExplanatoryStateMessage.Should().NotBeNullOrWhiteSpace();

        // Limited state
        var limited = _sut.BuildComparisonDashboard(loan, ComparisonTestData.SinglePayment(loan.Id), DashboardWindowKey.FullHistory);
        limited.Summary.ExplanatoryStateMessage.Should().NotBeNullOrWhiteSpace();

        // Ready state
        var ready = _sut.BuildComparisonDashboard(loan, ComparisonTestData.SixBaselinePayments(loan.Id), DashboardWindowKey.FullHistory);
        ready.Summary.ExplanatoryStateMessage.Should().NotBeNullOrWhiteSpace();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // T023: Windowing and series alignment (US2)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Window_selector_returns_all_four_available_windows()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.AvailableWindows.Should().HaveCount(4);
        result.AvailableWindows.Select(w => w.Key).Should().Contain(
        [
            DashboardWindowKey.FullHistory,
            DashboardWindowKey.Trailing6Months,
            DashboardWindowKey.Trailing12Months,
            DashboardWindowKey.YearToDate,
        ]);
    }

    [Fact]
    public void Active_window_matches_requested_window_key()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.Trailing6Months);

        result.ActiveWindow.Key.Should().Be(DashboardWindowKey.Trailing6Months);
        result.ActiveWindow.Label.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Balance_and_cost_series_share_the_same_dates()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.BalanceSeries.Select(p => p.Date)
            .Should().BeEquivalentTo(result.CostSeries.Select(p => p.Date));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // T033: Milestones, savings indicators, and limited-data states (US3)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Milestones_contains_divergence_start_when_extra_principal_present()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.MixedPayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        result.Milestones.Should().NotBeEmpty();
        result.Milestones.Any(m => m.Type == MilestoneType.DivergenceStart).Should().BeTrue();
    }

    [Fact]
    public void No_milestones_when_baseline_only_payments()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        // No extra principal → no divergence milestone
        result.Milestones.Should().NotContain(m => m.Type == MilestoneType.DivergenceStart);
    }

    [Fact]
    public void Savings_indicator_cumulative_interest_avoided_is_null_when_no_divergence()
    {
        var loan = ComparisonTestData.StandardLoan();
        var payments = ComparisonTestData.SixBaselinePayments(loan.Id);

        var result = _sut.BuildComparisonDashboard(loan, payments, DashboardWindowKey.FullHistory);

        // Baseline-only: no interest avoided
        result.Summary.CumulativeInterestAvoided.Should().BeNull();
    }
}
