using System.Net;
using System.Net.Http.Json;
using DebtDash.Web.Api.Contracts;
using DebtDash.Web.IntegrationTests.TestInfrastructure;
using FluentAssertions;

namespace DebtDash.Web.IntegrationTests.Api;

public class LoanEndpointsTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public LoanEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Get_loan_returns_404_when_no_loan_configured()
    {
        var response = await _client.GetAsync("/api/loan");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_loan_creates_new_profile()
    {
        var request = new LoanProfileUpsertRequest(
            InitialPrincipal: 250000m,
            AnnualRate: 6.5m,
            TermMonths: 240,
            StartDate: new DateOnly(2024, 3, 1),
            FixedMonthlyCosts: 100m,
            CurrencyCode: "MXN");

        var response = await _client.PutAsJsonAsync("/api/loan", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loan = await response.Content.ReadFromJsonAsync<LoanProfileResponse>();
        loan.Should().NotBeNull();
        loan!.InitialPrincipal.Should().Be(250000m);
        loan.AnnualRate.Should().Be(6.5m);
        loan.TermMonths.Should().Be(240);
        loan.CurrencyCode.Should().Be("MXN");
    }

    [Fact]
    public async Task Put_loan_updates_existing_profile()
    {
        var create = new LoanProfileUpsertRequest(200000m, 5m, 180, new DateOnly(2024, 1, 1), 0m, "USD");
        await _client.PutAsJsonAsync("/api/loan", create);

        var update = new LoanProfileUpsertRequest(200000m, 4.5m, 180, new DateOnly(2024, 1, 1), 0m, "USD");
        var response = await _client.PutAsJsonAsync("/api/loan", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loan = await response.Content.ReadFromJsonAsync<LoanProfileResponse>();
        loan!.AnnualRate.Should().Be(4.5m);
    }

    [Fact]
    public async Task Put_loan_returns_validation_error_for_negative_principal()
    {
        var request = new LoanProfileUpsertRequest(-1m, 5m, 180, new DateOnly(2024, 1, 1), 0m, "USD");
        var response = await _client.PutAsJsonAsync("/api/loan", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_loan_returns_saved_profile()
    {
        var request = new LoanProfileUpsertRequest(150000m, 7m, 120, new DateOnly(2025, 6, 1), 25m, "USD");
        await _client.PutAsJsonAsync("/api/loan", request);

        var response = await _client.GetAsync("/api/loan");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loan = await response.Content.ReadFromJsonAsync<LoanProfileResponse>();
        loan!.InitialPrincipal.Should().Be(150000m);
        loan.TermMonths.Should().Be(120);
    }
}
