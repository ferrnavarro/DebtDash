using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Api;

public class PaymentEndpointsTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentEndpointsTests()
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
        var response = await _client.PutAsJsonAsync("/api/loan", loan);
        response.EnsureSuccessStatusCode();
    }

    private static object MakePayment(string date = "2024-02-15", decimal total = 1500m,
        decimal principal = 1000m, decimal interest = 450m, decimal fees = 50m)
    {
        return new
        {
            paymentDate = date,
            totalPaid = total,
            principalPaid = principal,
            interestPaid = interest,
            feesPaid = fees,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        };
    }

    [Fact]
    public async Task Create_payment_returns_201_with_calculated_fields()
    {
        await SeedLoan();
        var payment = MakePayment();

        var response = await _client.PostAsJsonAsync("/api/payments", payment);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(result);
        Assert.True(result.DaysSincePreviousPayment > 0);
        Assert.True(result.RemainingBalanceAfterPayment < 200000m);
    }

    [Fact]
    public async Task Create_payment_without_loan_returns_400()
    {
        var payment = MakePayment();

        var response = await _client.PostAsJsonAsync("/api/payments", payment);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_payments_returns_paginated_list()
    {
        await SeedLoan();
        await _client.PostAsJsonAsync("/api/payments", MakePayment("2024-02-15"));
        await _client.PostAsJsonAsync("/api/payments", MakePayment("2024-03-15"));

        var response = await _client.GetAsync("/api/payments?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaymentListResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Update_payment_recalculates_chain()
    {
        await SeedLoan();
        var createResponse = await _client.PostAsJsonAsync("/api/payments", MakePayment());
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(created);

        var updated = MakePayment(total: 2000m, principal: 1500m, interest: 450m, fees: 50m);
        var response = await _client.PutAsJsonAsync($"/api/payments/{created.Id}", updated);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(result);
        Assert.Equal(1500m, result.PrincipalPaid);
    }

    [Fact]
    public async Task Delete_payment_returns_204()
    {
        await SeedLoan();
        var createResponse = await _client.PostAsJsonAsync("/api/payments", MakePayment());
        var created = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(created);

        var response = await _client.DeleteAsync($"/api/payments/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_nonexistent_payment_returns_404()
    {
        await SeedLoan();
        var response = await _client.DeleteAsync($"/api/payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_payment_with_mismatched_components_returns_400()
    {
        await SeedLoan();
        var bad = MakePayment(total: 1500m, principal: 1000m, interest: 300m, fees: 50m); // 1350 != 1500

        var response = await _client.PostAsJsonAsync("/api/payments", bad);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // DTO classes for deserialization
    private record PaymentResponse(
        Guid Id, string PaymentDate, decimal TotalPaid, decimal PrincipalPaid,
        decimal InterestPaid, decimal FeesPaid, int DaysSincePreviousPayment,
        decimal RemainingBalanceAfterPayment, decimal CalculatedRealRate,
        bool ManualRateOverrideEnabled, decimal? ManualRateOverride,
        RateVarianceResponse? RateVariance);

    private record RateVarianceResponse(
        decimal CalculatedRate, decimal? StatedOrOverrideRate,
        decimal VarianceAbsolute, decimal VarianceBasisPoints, bool IsFlagged);

    private record PaymentListResponse(List<PaymentResponse> Items, int Page, int PageSize, int TotalItems);
}
