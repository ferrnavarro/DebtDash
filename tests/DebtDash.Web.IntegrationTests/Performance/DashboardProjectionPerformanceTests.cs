using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Performance;

public class DashboardProjectionPerformanceTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardProjectionPerformanceTests()
    {
        _factory = new DebtDashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SeedData()
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

        for (var i = 0; i < 12; i++)
        {
            await _client.PostAsJsonAsync("/api/payments", new
            {
                paymentDate = new DateOnly(2024, 2 + (i / 2), 1 + (i % 28)).ToString("yyyy-MM-dd"),
                totalPaid = 1500m,
                principalPaid = 1000m,
                interestPaid = 450m,
                feesPaid = 50m,
                manualRateOverrideEnabled = false,
                manualRateOverride = (decimal?)null,
            });
        }
    }

    [Fact]
    public async Task Get_dashboard_responds_within_300ms_p95()
    {
        await SeedData();

        var timings = new List<long>();
        for (var i = 0; i < 20; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await _client.GetAsync("/api/dashboard");
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            timings.Add(sw.ElapsedMilliseconds);
        }

        timings.Sort();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[p95Index];

        Assert.True(p95 < 300, $"GET /api/dashboard p95 latency was {p95}ms, expected < 300ms");
    }

    [Fact]
    public async Task Get_projection_responds_within_300ms_p95()
    {
        await SeedData();

        var timings = new List<long>();
        for (var i = 0; i < 20; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await _client.GetAsync("/api/projections/true-end-date");
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            timings.Add(sw.ElapsedMilliseconds);
        }

        timings.Sort();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[p95Index];

        Assert.True(p95 < 300, $"GET /api/projections/true-end-date p95 latency was {p95}ms, expected < 300ms");
    }
}
