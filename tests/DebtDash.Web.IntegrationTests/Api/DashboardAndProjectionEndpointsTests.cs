using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Api;

public class DashboardAndProjectionEndpointsTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardAndProjectionEndpointsTests()
    {
        _factory = new DebtDashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SeedLoanAndPayments()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-02-15",
            totalPaid = 1500m,
            principalPaid = 1000m,
            interestPaid = 450m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-03-15",
            totalPaid = 1500m,
            principalPaid = 1050m,
            interestPaid = 400m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });
    }

    [Fact]
    public async Task Get_dashboard_without_loan_returns_404()
    {
        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_dashboard_returns_aggregated_metrics()
    {
        await SeedLoanAndPayments();

        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Dashboard now returns comparison response
        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        // With 2 payments, should return ready state
        Assert.Equal("ready", result.State);
        Assert.NotNull(result.Summary);
        Assert.Equal(4, result.AvailableWindows.Count);
    }

    [Fact]
    public async Task Get_projection_without_loan_returns_404()
    {
        var response = await _client.GetAsync("/api/projections/true-end-date");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_projection_returns_snapshot()
    {
        await SeedLoanAndPayments();

        var response = await _client.GetAsync("/api/projections/true-end-date");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProjectionDto>();
        Assert.NotNull(result);
        Assert.True(result.RemainingMonthsEstimate > 0);
        Assert.True(result.PrincipalVelocity > 0);
        Assert.True(result.BaselineRemainingMonths > 0);
    }

    [Fact]
    public async Task Get_projection_without_payments_returns_baseline()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        var response = await _client.GetAsync("/api/projections/true-end-date");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProjectionDto>();
        Assert.NotNull(result);
        Assert.Equal(360m, result.RemainingMonthsEstimate);
        Assert.Equal(0m, result.DeltaMonthsVsBaseline);
    }

    private record DashboardDto(
        decimal TotalInterestPaid,
        decimal TotalCapitalPaid,
        decimal AverageRealRateWeighted,
        decimal TimeRemainingMonths,
        int OriginalTermMonths,
        List<TrendPoint> PrincipalInterestTrendSeries,
        List<CountdownPoint> DebtCountdownSeries);

    private record TrendPoint(string Date, decimal PrincipalPaid, decimal InterestPaid);
    private record CountdownPoint(string Date, decimal RemainingBalance);

    private record ProjectionDto(
        string PredictedEndDate,
        decimal RemainingMonthsEstimate,
        decimal PrincipalVelocity,
        decimal BaselineRemainingMonths,
        decimal DeltaMonthsVsBaseline);

    // ──────────────────────────────────────────────────────────────────────────
    // T014: Dashboard summary integration coverage for comparison endpoint (US1)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_dashboard_returns_comparison_response_with_no_payments()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("empty", result.State);
        Assert.Equal("insufficientData", result.Summary.CurrentStatus);
        Assert.NotNull(result.Summary.ExplanatoryStateMessage);
        Assert.Empty(result.BalanceSeries);
        Assert.Equal(4, result.AvailableWindows.Count);
    }

    [Fact]
    public async Task Get_dashboard_returns_ready_state_with_payments()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithMixedPayments()
            .BuildAsync();

        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("ready", result.State);
        Assert.NotEmpty(result.BalanceSeries);
        Assert.NotEmpty(result.CostSeries);
        Assert.NotNull(result.Summary);
        Assert.Equal("fullHistory", result.ActiveWindow.Key);
    }

    [Fact]
    public async Task Get_dashboard_with_window_param_returns_correct_active_window()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithSixBaselinePayments()
            .BuildAsync();

        var response = await _client.GetAsync("/api/dashboard?window=trailing-6-months");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("trailing6Months", result.ActiveWindow.Key);
    }

    [Fact]
    public async Task Get_dashboard_summary_is_ahead_when_extra_principal_paid()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithMixedPayments()
            .BuildAsync();

        var response = await _client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();

        Assert.NotNull(result);
        Assert.Equal("ahead", result.Summary.CurrentStatus);
        Assert.NotNull(result.Summary.ExplanatoryStateMessage);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // T024: Windowed dashboard series integration coverage (US2)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_dashboard_full_history_returns_all_payments_in_series()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithMixedPayments()
            .BuildAsync();

        var response = await _client.GetAsync("/api/dashboard?window=full-history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("fullHistory", result.ActiveWindow.Key);
        Assert.NotEmpty(result.BalanceSeries);
        Assert.NotEmpty(result.CostSeries);
        // All series dates must fall within the window range
        foreach (var point in result.BalanceSeries)
        {
            Assert.True(string.Compare(point.Date, result.ActiveWindow.RangeStart, StringComparison.Ordinal) >= 0);
            Assert.True(string.Compare(point.Date, result.ActiveWindow.RangeEnd, StringComparison.Ordinal) <= 0);
        }
    }

    [Fact]
    public async Task Get_dashboard_trailing6months_window_returns_shorter_series_than_full_history()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithMixedPayments()
            .BuildAsync();

        var fullResponse = await _client.GetAsync("/api/dashboard?window=full-history");
        var trailingResponse = await _client.GetAsync("/api/dashboard?window=trailing-6-months");

        var fullResult = await fullResponse.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        var trailingResult = await trailingResponse.Content.ReadFromJsonAsync<ComparisonResponseDto>();

        Assert.NotNull(fullResult);
        Assert.NotNull(trailingResult);
        Assert.Equal("trailing6Months", trailingResult.ActiveWindow.Key);
        // Trailing-6-month series should not exceed full-history length
        Assert.True(trailingResult.BalanceSeries.Count <= fullResult.BalanceSeries.Count);
    }

    [Fact]
    public async Task Get_dashboard_unknown_window_param_defaults_to_full_history()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithMixedPayments()
            .BuildAsync();

        var response = await _client.GetAsync("/api/dashboard?window=unknown-value");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("fullHistory", result.ActiveWindow.Key);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // T034: Milestone and limited-data payload fields integration coverage (US3)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_dashboard_with_extra_principal_payments_returns_milestones()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder
            .WithLoan(200000m)
            .WithMixedPayments()
            .BuildAsync();

        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        // Extra-principal payments should generate at least a divergence-start milestone
        Assert.NotEmpty(result.Milestones);
        var milestone = result.Milestones[0];
        Assert.NotNull(milestone.Type);
        Assert.NotNull(milestone.Title);
        Assert.NotNull(milestone.Description);
    }

    [Fact]
    public async Task Get_dashboard_with_one_payment_returns_limited_data_state()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-02-15",
            totalPaid = 1500m,
            principalPaid = 596m,
            interestPaid = 854m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ComparisonResponseDto>();
        Assert.NotNull(result);
        Assert.Equal("limitedData", result.State);
        Assert.NotNull(result.Summary.ExplanatoryStateMessage);
    }

    [Fact]
    public async Task Get_dashboard_limited_data_state_has_no_milestones()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-02-15",
            totalPaid = 1500m,
            principalPaid = 596m,
            interestPaid = 854m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        var result = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<ComparisonResponseDto>();

        Assert.NotNull(result);
        Assert.Empty(result.Milestones);
    }

    // DTOs for comparison endpoint deserialization
    private record ComparisonResponseDto(
        ComparisonSummaryDto Summary,
        List<TimelinePointDto> BalanceSeries,
        List<TimelinePointDto> CostSeries,
        List<MilestoneDto> Milestones,
        List<WindowDto> AvailableWindows,
        WindowDto ActiveWindow,
        string State);

    private record ComparisonSummaryDto(
        string WindowKey,
        string CurrentStatus,
        decimal? MonthsSaved,
        decimal? ProjectedPayoffDateDelta,
        decimal? RemainingBalanceDelta,
        decimal? CumulativeInterestAvoided,
        string? FirstMeaningfulDivergenceDate,
        string LastRecalculatedAt,
        string ExplanatoryStateMessage);

    private record TimelinePointDto(
        string Date,
        decimal ActualRemainingBalance,
        decimal BaselineRemainingBalance,
        decimal BalanceDelta,
        decimal InterestDelta,
        bool ContainsExtraPrincipalEffect);

    private record MilestoneDto(string Type, string Date, string Title, string Description, decimal? Value);

    private record WindowDto(string Key, string Label, string RangeStart, string RangeEnd);
}
