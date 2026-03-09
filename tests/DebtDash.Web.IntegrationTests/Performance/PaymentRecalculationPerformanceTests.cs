using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Performance;

public class PaymentRecalculationPerformanceTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentRecalculationPerformanceTests()
    {
        _factory = new DebtDashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SeedLoan()
    {
        var loan = new
        {
            initialPrincipal = 200000m,
            annualRate = 5.5m,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        };
        await _client.PutAsJsonAsync("/api/loan", loan);
    }

    [Fact]
    public async Task Payment_create_with_recalculation_responds_within_2000ms()
    {
        await SeedLoan();

        // Seed some existing payments
        for (var i = 0; i < 10; i++)
        {
            var payment = new
            {
                paymentDate = new DateOnly(2024, 2 + (i / 2), 1 + (i % 28)).ToString("yyyy-MM-dd"),
                totalPaid = 1500m,
                principalPaid = 1000m,
                interestPaid = 450m,
                feesPaid = 50m,
                manualRateOverrideEnabled = false,
                manualRateOverride = (decimal?)null,
            };
            await _client.PostAsJsonAsync("/api/payments", payment);
        }

        // Measure the create+recalculation time
        var timings = new List<long>();
        for (var i = 0; i < 5; i++)
        {
            var payment = new
            {
                paymentDate = new DateOnly(2024, 8 + i, 15).ToString("yyyy-MM-dd"),
                totalPaid = 1500m,
                principalPaid = 1000m,
                interestPaid = 450m,
                feesPaid = 50m,
                manualRateOverrideEnabled = false,
                manualRateOverride = (decimal?)null,
            };

            var sw = Stopwatch.StartNew();
            var response = await _client.PostAsJsonAsync("/api/payments", payment);
            sw.Stop();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            timings.Add(sw.ElapsedMilliseconds);
        }

        timings.Sort();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[p95Index];

        Assert.True(p95 < 2000, $"Payment create+recalc p95 latency was {p95}ms, expected < 2000ms");
    }

    [Fact]
    public async Task Payment_delete_with_recalculation_responds_within_2000ms()
    {
        await SeedLoan();

        // Seed payments
        var ids = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var payment = new
            {
                paymentDate = new DateOnly(2024, 2 + (i / 2), 1 + (i % 28)).ToString("yyyy-MM-dd"),
                totalPaid = 1500m,
                principalPaid = 1000m,
                interestPaid = 450m,
                feesPaid = 50m,
                manualRateOverrideEnabled = false,
                manualRateOverride = (decimal?)null,
            };
            var resp = await _client.PostAsJsonAsync("/api/payments", payment);
            var body = await resp.Content.ReadFromJsonAsync<PaymentIdResponse>();
            if (body is not null) ids.Add(body.Id.ToString());
        }

        var timings = new List<long>();
        foreach (var id in ids.Take(5))
        {
            var sw = Stopwatch.StartNew();
            var response = await _client.DeleteAsync($"/api/payments/{id}");
            sw.Stop();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            timings.Add(sw.ElapsedMilliseconds);
        }

        timings.Sort();
        var p95Index = (int)Math.Ceiling(timings.Count * 0.95) - 1;
        var p95 = timings[p95Index];

        Assert.True(p95 < 2000, $"Payment delete+recalc p95 latency was {p95}ms, expected < 2000ms");
    }

    private record PaymentIdResponse(Guid Id);
}
