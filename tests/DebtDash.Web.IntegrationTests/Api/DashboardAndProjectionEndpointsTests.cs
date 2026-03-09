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

        var result = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(result);
        Assert.Equal(850m, result.TotalInterestPaid);   // 450 + 400
        Assert.Equal(2050m, result.TotalCapitalPaid);    // 1000 + 1050
        Assert.True(result.AverageRealRateWeighted > 0);
        Assert.Equal(360, result.OriginalTermMonths);
        Assert.Equal(2, result.PrincipalInterestTrendSeries.Count);
        Assert.Equal(2, result.DebtCountdownSeries.Count);
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
}
