using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Regression;

/// <summary>
/// T042 [US3] Regression: savings and milestone field consistency across comparison payloads.
/// Ensures that milestone enumeration and savings summary remain stable under various
/// payment scenarios and do not regress silently.
/// </summary>
public class DashboardComparisonRegressionTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardComparisonRegressionTests()
    {
        _factory = new DebtDashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Regression_extra_principal_payments_produce_positive_balance_delta()
    {
        // Arrange: loan with consistent extra principal each month
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200_000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        var payments = new[]
        {
            new { paymentDate = "2024-02-15", totalPaid = 2000m, principalPaid = 1100m, interestPaid = 854m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
            new { paymentDate = "2024-03-15", totalPaid = 2000m, principalPaid = 1150m, interestPaid = 804m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
            new { paymentDate = "2024-04-15", totalPaid = 2000m, principalPaid = 1200m, interestPaid = 754m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
        };
        foreach (var p in payments)
            await _client.PostAsJsonAsync("/api/payments", p);

        // Act
        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DashboardRegressionDto>();
        Assert.NotNull(result);

        // Balance delta must be positive (ahead of baseline) because extra principal was paid
        Assert.True(result.Summary.RemainingBalanceDelta > 0,
            $"Expected positive balance delta, got {result.Summary.RemainingBalanceDelta}");
        Assert.Equal("ahead", result.Summary.CurrentStatus);
    }

    [Fact]
    public async Task Regression_below_baseline_payments_produce_negative_or_zero_balance_delta()
    {
        // Arrange: loan where principal paid is well below amortization schedule baseline
        // For $200k / 5.5% / 360mo, the baseline monthly principal payment in month 1 is ~$219.
        // Paying $50/month principal is clearly below baseline.
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200_000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        var payments = new[]
        {
            new { paymentDate = "2024-02-15", totalPaid = 1016m, principalPaid = 50m, interestPaid = 920m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
            new { paymentDate = "2024-03-15", totalPaid = 1016m, principalPaid = 50m, interestPaid = 920m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
            new { paymentDate = "2024-04-15", totalPaid = 1016m, principalPaid = 50m, interestPaid = 920m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
        };
        foreach (var p in payments)
            await _client.PostAsJsonAsync("/api/payments", p);

        var response = await _client.GetAsync("/api/dashboard");
        var result = await response.Content.ReadFromJsonAsync<DashboardRegressionDto>();

        Assert.NotNull(result);
        // Balance delta is 0 or behind when paying less principal than baseline
        Assert.True(result.Summary.RemainingBalanceDelta <= 0,
            $"Expected non-positive balance delta, got {result.Summary.RemainingBalanceDelta}");
        Assert.True(result.Summary.CurrentStatus is "behind" or "onTrack",
            $"Expected behind or onTrack, got {result.Summary.CurrentStatus}");
    }

    [Fact]
    public async Task Regression_milestone_types_are_known_values()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder.WithLoan(200_000m).WithMixedPayments().BuildAsync();

        var result = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<DashboardRegressionDto>();

        Assert.NotNull(result);

        var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "divergenceStart", "highestBalanceGap", "highestInterestSavings", "earlyPayoff"
        };

        foreach (var milestone in result.Milestones)
        {
            Assert.Contains(milestone.Type, validTypes);
        }
    }

    [Fact]
    public async Task Regression_available_windows_always_returns_four_entries()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder.WithLoan(200_000m).WithMixedPayments().BuildAsync();

        var result = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<DashboardRegressionDto>();

        Assert.NotNull(result);
        Assert.Equal(4, result.AvailableWindows.Count);

        var expectedKeys = new[] { "fullHistory", "trailing6Months", "trailing12Months", "yearToDate" };
        var actualKeys = result.AvailableWindows.Select(w => w.Key).ToList();
        foreach (var key in expectedKeys)
            Assert.Contains(key, actualKeys);
    }

    [Fact]
    public async Task Regression_balance_series_dates_are_chronological()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder.WithLoan(200_000m).WithMixedPayments().BuildAsync();

        var result = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<DashboardRegressionDto>();

        Assert.NotNull(result);
        Assert.NotEmpty(result.BalanceSeries);

        for (int i = 1; i < result.BalanceSeries.Count; i++)
        {
            Assert.True(
                string.Compare(result.BalanceSeries[i].Date, result.BalanceSeries[i - 1].Date, StringComparison.Ordinal) >= 0,
                $"Balance series not in order at index {i}: {result.BalanceSeries[i - 1].Date} → {result.BalanceSeries[i].Date}");
        }
    }

    // Regression DTOs
    private record DashboardRegressionDto(
        RegressionSummaryDto Summary,
        List<RegressionSeriesPoint> BalanceSeries,
        List<RegressionMilestone> Milestones,
        List<RegressionWindow> AvailableWindows,
        string State);

    private record RegressionSummaryDto(
        string CurrentStatus,
        decimal? RemainingBalanceDelta,
        decimal? CumulativeInterestAvoided,
        string ExplanatoryStateMessage);

    private record RegressionSeriesPoint(string Date);

    private record RegressionMilestone(string Type, string Title, string Description);

    private record RegressionWindow(string Key, string Label);
}
