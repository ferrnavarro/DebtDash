using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Performance;

public class LoanEndpointsPerformanceTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoanEndpointsPerformanceTests()
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
    public async Task Get_loan_responds_within_300ms_p95()
    {
        // Seed a loan first
        var request = new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        };
        await _client.PutAsJsonAsync("/api/loan", request);

        var timings = new List<long>();
        for (var i = 0; i < 20; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await _client.GetAsync("/api/loan");
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            timings.Add(sw.ElapsedMilliseconds);
        }

        timings.Sort();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[p95Index];

        Assert.True(p95 < 300, $"GET /api/loan p95 latency was {p95}ms, expected < 300ms");
    }

    [Fact]
    public async Task Put_loan_responds_within_2000ms_p95()
    {
        var timings = new List<long>();
        for (var i = 0; i < 20; i++)
        {
            var request = new
            {
                initialPrincipal = 200000m + i,
                annualRate = 5.5m,
                termMonths = 360,
                startDate = "2024-01-15",
                fixedMonthlyCosts = 50m,
                currencyCode = "USD"
            };

            var sw = Stopwatch.StartNew();
            var response = await _client.PutAsJsonAsync("/api/loan", request);
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            timings.Add(sw.ElapsedMilliseconds);
        }

        timings.Sort();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[p95Index];

        Assert.True(p95 < 2000, $"PUT /api/loan p95 latency was {p95}ms, expected < 2000ms");
    }
}
