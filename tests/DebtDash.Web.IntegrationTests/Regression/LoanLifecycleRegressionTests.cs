using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.IntegrationTests.TestInfrastructure;

namespace DebtDash.Web.IntegrationTests.Regression;

/// <summary>
/// End-to-end regression test covering the full loan lifecycle:
/// Configure loan → Add payments → View dashboard → Get projection → Update payment → Verify recalculation
/// </summary>
public class LoanLifecycleRegressionTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LoanLifecycleRegressionTests()
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
    public async Task Full_loan_lifecycle_regression()
    {
        // Step 1: No loan → 404
        var noLoan = await _client.GetAsync("/api/loan");
        Assert.Equal(HttpStatusCode.NotFound, noLoan.StatusCode);

        // Step 2: Configure loan
        var loanResponse = await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 100000m,
            annualRate = 6.0m,
            termMonths = 120,
            startDate = "2024-01-01",
            fixedMonthlyCosts = 25m,
            currencyCode = "USD"
        });
        Assert.Equal(HttpStatusCode.OK, loanResponse.StatusCode);

        // Step 3: Verify loan persisted
        var getLoan = await _client.GetAsync("/api/loan");
        Assert.Equal(HttpStatusCode.OK, getLoan.StatusCode);
        var loan = await getLoan.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);
        Assert.Equal(100000m, loan.InitialPrincipal);

        // Step 4: Add first payment
        var pay1 = await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-02-01",
            totalPaid = 1500m,
            principalPaid = 1000m,
            interestPaid = 450m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });
        Assert.Equal(HttpStatusCode.Created, pay1.StatusCode);
        var payment1 = await pay1.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.NotNull(payment1);
        Assert.Equal(99000m, payment1.RemainingBalanceAfterPayment);
        Assert.True(payment1.DaysSincePreviousPayment > 0);

        // Step 5: Add second payment
        var pay2 = await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate = "2024-03-01",
            totalPaid = 1500m,
            principalPaid = 1050m,
            interestPaid = 400m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });
        Assert.Equal(HttpStatusCode.Created, pay2.StatusCode);

        // Step 6: List payments
        var listResponse = await _client.GetAsync("/api/payments?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<PaymentListDto>();
        Assert.NotNull(list);
        Assert.Equal(2, list.TotalItems);

        // Step 7: Get dashboard - should have comparison data
        var dashResponse = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashResponse.StatusCode);
        var dash = await dashResponse.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dash);
        Assert.Equal("ready", dash.State);
        Assert.NotEmpty(dash.BalanceSeries);
        Assert.Equal(4, dash.AvailableWindows.Count);

        // Step 8: Get projection
        var projResponse = await _client.GetAsync("/api/projections/true-end-date");
        Assert.Equal(HttpStatusCode.OK, projResponse.StatusCode);
        var proj = await projResponse.Content.ReadFromJsonAsync<ProjectionDto>();
        Assert.NotNull(proj);
        Assert.True(proj.RemainingMonthsEstimate > 0);
        Assert.True(proj.PrincipalVelocity > 0);

        // Step 9: Update first payment (increase principal)
        var updateResponse = await _client.PutAsJsonAsync($"/api/payments/{payment1.Id}", new
        {
            paymentDate = "2024-02-01",
            totalPaid = 2000m,
            principalPaid = 1500m,
            interestPaid = 450m,
            feesPaid = 50m,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // Step 10: Verify dashboard reflects update (still ready state)
        var dash2Response = await _client.GetAsync("/api/dashboard");
        var dash2 = await dash2Response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dash2);
        Assert.Equal("ready", dash2.State);
        Assert.NotEmpty(dash2.BalanceSeries);

        // Step 11: Delete second payment
        var deleteResponse = await _client.DeleteAsync($"/api/payments/{list.Items[1].Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Step 12: Verify only 1 payment remains
        var listAfterDelete = await _client.GetAsync("/api/payments?page=1&pageSize=50");
        var updatedList = await listAfterDelete.Content.ReadFromJsonAsync<PaymentListDto>();
        Assert.NotNull(updatedList);
        Assert.Equal(1, updatedList.TotalItems);

        // Step 13: Update loan terms
        var updateLoan = await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = 100000m,
            annualRate = 5.5m, // Rate changed
            termMonths = 120,
            startDate = "2024-01-01",
            fixedMonthlyCosts = 25m,
            currencyCode = "USD"
        });
        Assert.Equal(HttpStatusCode.OK, updateLoan.StatusCode);
        var updatedLoan = await updateLoan.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(updatedLoan);
        Assert.Equal(5.5m, updatedLoan.AnnualRate);
    }

    private record LoanDto(Guid Id, decimal InitialPrincipal, decimal AnnualRate,
        int TermMonths, string StartDate, decimal FixedMonthlyCosts, string CurrencyCode);

    private record PaymentDto(Guid Id, string PaymentDate, decimal TotalPaid,
        decimal PrincipalPaid, decimal InterestPaid, decimal FeesPaid,
        int DaysSincePreviousPayment, decimal RemainingBalanceAfterPayment,
        decimal CalculatedRealRate, bool ManualRateOverrideEnabled, decimal? ManualRateOverride);

    private record PaymentListDto(List<PaymentDto> Items, int Page, int PageSize, int TotalItems);

    private record DashboardDto(
        string State,
        List<LifecycleSeriesPoint> BalanceSeries,
        List<LifecycleWindow> AvailableWindows);

    private record LifecycleSeriesPoint(string Date);
    private record LifecycleWindow(string Key);

    private record ProjectionDto(string PredictedEndDate, decimal RemainingMonthsEstimate,
        decimal PrincipalVelocity, decimal BaselineRemainingMonths, decimal DeltaMonthsVsBaseline);
}
