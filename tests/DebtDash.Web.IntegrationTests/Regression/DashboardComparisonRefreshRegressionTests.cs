using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Regression;

/// <summary>
/// T045 Cross-story regression: payment edits and dashboard refresh consistency.
/// Verifies that adding, then immediately fetching the dashboard reflects the latest
/// payment state and does not return stale data.
/// </summary>
public class DashboardComparisonRefreshRegressionTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardComparisonRefreshRegressionTests()
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
    public async Task Dashboard_reflects_newly_added_payment_immediately()
    {
        // Arrange: seed loan and one baseline payment
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200_000m,
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
            interestPaid = 858m,
            feesPaid = 46m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        // Verify initial limited-data state (1 payment below threshold)
        var first = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<RefreshRegressionDto>();
        Assert.NotNull(first);
        Assert.Equal("limitedData", first.State);

        // Act: add second payment to cross the 2-payment threshold
        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-03-15",
            totalPaid = 2000m,
            principalPaid = 1150m,
            interestPaid = 804m,
            feesPaid = 46m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        // Assert: state should now be ready
        var second = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<RefreshRegressionDto>();
        Assert.NotNull(second);
        Assert.Equal("ready", second.State);
        Assert.NotEmpty(second.BalanceSeries);
    }

    [Fact]
    public async Task Dashboard_state_transitions_from_empty_to_limited_data_after_first_payment()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200_000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        // Empty state
        var empty = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<RefreshRegressionDto>();
        Assert.NotNull(empty);
        Assert.Equal("empty", empty.State);

        // Add one payment → limited-data
        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-02-15",
            totalPaid = 1500m,
            principalPaid = 596m,
            interestPaid = 858m,
            feesPaid = 46m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        var limited = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<RefreshRegressionDto>();
        Assert.NotNull(limited);
        Assert.Equal("limitedData", limited.State);
    }

    [Fact]
    public async Task Dashboard_summary_status_updates_when_extra_principal_added()
    {
        await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 200_000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        });

        // Seed with below-baseline payments (principal $50/mo well below ~$219 baseline)
        var baselinePayments = new[]
        {
            new { paymentDate = "2024-02-15", totalPaid = 1016m, principalPaid = 50m, interestPaid = 920m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
            new { paymentDate = "2024-03-15", totalPaid = 1016m, principalPaid = 50m, interestPaid = 920m, feesPaid = 46m, manualRateOverrideEnabled = false, manualRateOverride = (decimal?)null },
        };
        foreach (var p in baselinePayments)
            await _client.PostAsJsonAsync("/api/payments", p);

        var beforeExtra = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<RefreshRegressionDto>();
        Assert.NotNull(beforeExtra);
        // paying below baseline → behind or onTrack
        Assert.True(beforeExtra.Summary.CurrentStatus is "behind" or "onTrack",
            $"Expected behind/onTrack before extra payment, got {beforeExtra.Summary.CurrentStatus}");

        // Add a payment with large extra principal
        await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-04-15",
            totalPaid = 5000m,
            principalPaid = 4100m,
            interestPaid = 854m,
            feesPaid = 46m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });

        var afterExtra = await (await _client.GetAsync("/api/dashboard"))
            .Content.ReadFromJsonAsync<RefreshRegressionDto>();
        Assert.NotNull(afterExtra);
        Assert.Equal("ahead", afterExtra.Summary.CurrentStatus);
    }

    [Fact]
    public async Task Dashboard_window_switching_is_consistent_after_payments_added()
    {
        var builder = new ComparisonScenarioBuilder(_client);
        await builder.WithLoan(200_000m).WithMixedPayments().BuildAsync();

        // Sequential requests - avoids concurrent stream issues with in-memory test client
        var fullResponse = await _client.GetAsync("/api/dashboard?window=full-history");
        var trailingResponse = await _client.GetAsync("/api/dashboard?window=trailing-6-months");

        var full = await fullResponse.Content.ReadFromJsonAsync<RefreshRegressionDto>();
        var trailing = await trailingResponse.Content.ReadFromJsonAsync<RefreshRegressionDto>();

        Assert.NotNull(full);
        Assert.NotNull(trailing);
        Assert.Equal("fullHistory", full.ActiveWindow?.Key);
        Assert.Equal("trailing6Months", trailing.ActiveWindow?.Key);

        // Both should be ready state
        Assert.Equal("ready", full.State);
        Assert.Equal("ready", trailing.State);
    }

    // Refresh regression DTOs
    private record RefreshRegressionDto(
        RefreshSummaryDto Summary,
        List<RefreshSeriesPoint> BalanceSeries,
        RefreshWindowDto? ActiveWindow,
        string State);

    private record RefreshSummaryDto(string CurrentStatus);

    private record RefreshSeriesPoint(string Date);

    private record RefreshWindowDto(string Key);
}
