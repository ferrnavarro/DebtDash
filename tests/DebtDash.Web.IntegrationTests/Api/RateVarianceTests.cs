using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Api;

public class RateVarianceTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RateVarianceTests()
    {
        _factory = new DebtDashWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task SeedLoan(decimal annualRate = 5.5m)
    {
        var loan = new
        {
            initialPrincipal = 200000m,
            annualRate,
            termMonths = 360,
            startDate = "2024-01-15",
            fixedMonthlyCosts = 50m,
            currencyCode = "USD"
        };
        await _client.PutAsJsonAsync("/api/loan", loan);
    }

    [Fact]
    public async Task Payment_without_override_uses_loan_rate_for_variance()
    {
        await SeedLoan(5.5m);

        // Create a payment where actual interest closely matches 5.5% annual rate:
        // Expected: 200000 * 5.5% * 31/365 = 934.25
        // So interestPaid=934.25 should give calculatedRate ≈ 5.5, minimal variance
        var payment = new
        {
            paymentDate = "2024-02-15",
            totalPaid = 1984.25m,
            principalPaid = 1000m,
            interestPaid = 934.25m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        };

        var response = await _client.PostAsJsonAsync("/api/payments", payment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.RateVariance);
        // Calculated rate should be very close to 5.5, so variance should be small
        Assert.False(result.RateVariance.IsFlagged,
            $"Variance should not be flagged: calcRate={result.RateVariance.CalculatedRate}, " +
            $"basisPoints={result.RateVariance.VarianceBasisPoints}");
    }

    [Fact]
    public async Task Payment_with_significant_interest_deviation_flags_variance()
    {
        await SeedLoan(5.5m);

        // Interest that implies a significantly different rate (far above 5.5%)
        // 200000 * r/100 * 31/365 = 1200 => r = 1200 * 365 / (200000 * 31) = 7.064...
        // Variance from 5.5 = ~1.56 percentage points = ~156 basis points -> flagged
        var payment = new
        {
            paymentDate = "2024-02-15",
            totalPaid = 2250m,
            principalPaid = 1000m,
            interestPaid = 1200m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        };

        var response = await _client.PostAsJsonAsync("/api/payments", payment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.RateVariance);
        Assert.True(result.RateVariance.IsFlagged,
            $"Variance should be flagged: basisPoints={result.RateVariance.VarianceBasisPoints}");
    }

    [Fact]
    public async Task Manual_rate_override_is_used_for_variance_comparison()
    {
        await SeedLoan(5.5m);

        // Interest implies ~5.5% rate, but override is 5.0%, so variance is ~0.5 pp = 50 bp -> flagged
        var payment = new
        {
            paymentDate = "2024-02-15",
            totalPaid = 1984.25m,
            principalPaid = 1000m,
            interestPaid = 934.25m,
            feesPaid = 50m,
            manualRateOverrideEnabled = true,
            manualRateOverride = 5.0m
        };

        var response = await _client.PostAsJsonAsync("/api/payments", payment);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.RateVariance);
        // Stated is 5.0, calculated will be ~5.5 from interest, so variance > 5bp threshold
        Assert.True(result.RateVariance.IsFlagged);
        Assert.Equal(5.0m, result.RateVariance.StatedOrOverrideRate);
    }

    private record PaymentResponse(
        Guid Id, string PaymentDate, decimal TotalPaid, decimal PrincipalPaid,
        decimal InterestPaid, decimal FeesPaid, int DaysSincePreviousPayment,
        decimal RemainingBalanceAfterPayment, decimal CalculatedRealRate,
        bool ManualRateOverrideEnabled, decimal? ManualRateOverride,
        RateVarianceResponse? RateVariance);

    private record RateVarianceResponse(
        decimal CalculatedRate, decimal? StatedOrOverrideRate,
        decimal VarianceAbsolute, decimal VarianceBasisPoints, bool IsFlagged);
}
