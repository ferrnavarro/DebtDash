using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DebtDash.Web.Api.Contracts;
using DebtDash.Web.IntegrationTests.TestInfrastructure;
using FluentAssertions;

namespace DebtDash.Web.IntegrationTests.Api;

/// <summary>
/// T006 / T015 / T020 / T032: Integration tests for the payment calculator endpoints.
/// Tests the full HTTP → service → in-memory SQLite flow using DebtDashWebApplicationFactory.
/// </summary>
public class CalculatorEndpointsTests : IDisposable
{
    private readonly DebtDashWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    // JsonSerializerOptions that match the app's camelCase + enum-as-string configuration.
    private static readonly JsonSerializerOptions SerializerOptions = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return opts;
    }

    public CalculatorEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task SeedLoanAsync(decimal principal = 200_000m, decimal annualRate = 5.5m)
    {
        var response = await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = principal,
            annualRate,
            termMonths = 360,
            startDate = "2024-01-01",
            fixedMonthlyCosts = 0m,
            currencyCode = "USD",
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task SeedPaymentAsync(
        decimal principalPaid = 1_000m,
        decimal feesPaid = 75m,
        string paymentDate = "2026-02-15")
    {
        var response = await _client.PostAsJsonAsync("/api/payments", new
        {
            paymentDate,
            totalPaid = principalPaid + 500m + feesPaid,
            principalPaid,
            interestPaid = 500m,
            feesPaid,
            manualRateOverrideEnabled = false,
            manualRateOverride = (decimal?)null,
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    // ── T020: GET /api/calculator/default-fee ────────────────────────────────

    [Fact]
    public async Task Get_default_fee_returns_404_when_no_loan_configured()
    {
        var response = await _client.GetAsync("/api/calculator/default-fee");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_default_fee_returns_null_fields_when_ledger_is_empty()
    {
        await SeedLoanAsync();

        var response = await _client.GetAsync("/api/calculator/default-fee");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeAsync<FeeDefaultResponse>(response);
        body!.DefaultFeeAmount.Should().BeNull();
        body.SourcePaymentDate.Should().BeNull();
    }

    [Fact]
    public async Task Get_default_fee_returns_fee_and_date_from_most_recent_entry()
    {
        await SeedLoanAsync();
        await SeedPaymentAsync(principalPaid: 1_000m, feesPaid: 75m);

        var response = await _client.GetAsync("/api/calculator/default-fee");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeAsync<FeeDefaultResponse>(response);
        body!.DefaultFeeAmount.Should().Be(75m);
        body.SourcePaymentDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_default_fee_returns_zero_fee_when_entry_has_zero_fee()
    {
        await SeedLoanAsync();
        await SeedPaymentAsync(principalPaid: 1_000m, feesPaid: 0m);

        var response = await _client.GetAsync("/api/calculator/default-fee");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await DeserializeAsync<FeeDefaultResponse>(response);
        // zero is a valid fee — must not be treated as "no default"
        body!.DefaultFeeAmount.Should().Be(0m);
    }

    // ── T006: POST /api/calculator/schedule — 404 / 400 guards ───────────────

    [Fact]
    public async Task Post_schedule_returns_404_when_no_loan_configured()
    {
        var request = new PaymentScheduleRequest(
            PayoffDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12),
            FeeAmount: 0m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_schedule_returns_400_when_payoff_date_is_in_current_month()
    {
        await SeedLoanAsync();

        // Same month → DeriveRemainingPeriods returns 0
        var sameMonthDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new PaymentScheduleRequest(sameMonthDate, FeeAmount: 0m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_schedule_returns_400_when_fee_amount_is_negative()
    {
        await SeedLoanAsync();

        var request = new PaymentScheduleRequest(
            PayoffDate: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6),
            FeeAmount: -10m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── T006: POST /api/calculator/schedule — success path ───────────────────

    [Fact]
    public async Task Post_schedule_with_payment_entry_returns_200_using_ledger_rate()
    {
        await SeedLoanAsync(principal: 100_000m, annualRate: 5.5m);
        await SeedPaymentAsync(principalPaid: 5_000m, feesPaid: 75m);

        var payoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12);
        var request = new PaymentScheduleRequest(payoffDate, FeeAmount: 75m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await DeserializeAsync<PaymentScheduleResponse>(response);
        schedule.Should().NotBeNull();
        schedule!.Periods.Should().Be(12);
        schedule.Entries.Should().HaveCount(12);
        schedule.RateQuote.Source.Should().Be(RateSource.Ledger);
        schedule.RateQuote.IsFallback.Should().BeFalse();
        schedule.Entries[^1].RemainingBalance.Should().BeInRange(-0.01m, 0.01m);
    }

    [Fact]
    public async Task Post_schedule_period_count_matches_months_to_payoff_date()
    {
        await SeedLoanAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var payoffDate = today.AddMonths(24);
        var request = new PaymentScheduleRequest(payoffDate, FeeAmount: 0m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);
        var schedule = await DeserializeAsync<PaymentScheduleResponse>(response);

        schedule!.Periods.Should().Be(24);
        schedule.Entries.Should().HaveCount(24);
    }

    [Fact]
    public async Task Post_schedule_summary_totals_equal_sum_of_period_components()
    {
        await SeedLoanAsync(principal: 50_000m, annualRate: 6.0m);

        var payoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12);
        var request = new PaymentScheduleRequest(payoffDate, FeeAmount: 50m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);
        var schedule = await DeserializeAsync<PaymentScheduleResponse>(response);

        var expectedPrincipal = schedule!.Entries.Sum(e => e.PrincipalComponent);
        var expectedInterest = schedule.Entries.Sum(e => e.InterestComponent);
        var expectedFees = schedule.Entries.Sum(e => e.FeeComponent);

        schedule.Summary.TotalPrincipal.Should().BeApproximately(expectedPrincipal, 0.02m);
        schedule.Summary.TotalInterest.Should().BeApproximately(expectedInterest, 0.02m);
        schedule.Summary.TotalFees.Should().BeApproximately(expectedFees, 0.02m);
        schedule.Summary.TotalAmountPaid.Should().BeApproximately(
            schedule.Summary.TotalPrincipal + schedule.Summary.TotalInterest + schedule.Summary.TotalFees,
            0.02m);
    }

    [Fact]
    public async Task Post_schedule_null_fee_amount_defaults_to_ledger_fee()
    {
        await SeedLoanAsync(principal: 80_000m, annualRate: 5.0m);
        await SeedPaymentAsync(principalPaid: 2_000m, feesPaid: 75m);

        // Send null feeAmount — should default to ledger's 75m
        var payoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12);
        var request = new PaymentScheduleRequest(payoffDate, FeeAmount: null);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);
        var schedule = await DeserializeAsync<PaymentScheduleResponse>(response);

        schedule!.FeeAmountPerPeriod.Should().Be(75m);
        schedule.Entries.Should().AllSatisfy(e => e.FeeComponent.Should().Be(75m));
    }

    // ── T015: POST /api/calculator/schedule — baseline fallback path ──────────

    [Fact]
    public async Task Post_schedule_with_no_payments_uses_baseline_rate_from_loan_profile()
    {
        await SeedLoanAsync(annualRate: 6.0m);

        var payoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12);
        var request = new PaymentScheduleRequest(payoffDate, FeeAmount: 0m);

        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedule = await DeserializeAsync<PaymentScheduleResponse>(response);
        schedule!.RateQuote.Source.Should().Be(RateSource.Baseline);
        schedule.RateQuote.IsFallback.Should().BeTrue();
        schedule.RateQuote.AnnualRate.Should().Be(6.0m);
        schedule.RateQuote.FallbackReason.Should().NotBeNullOrEmpty();
    }

    // ── T032: 360-period performance budget p95 < 500 ms ─────────────────────

    [Fact]
    public async Task Post_schedule_360_periods_completes_within_500ms()
    {
        await SeedLoanAsync(principal: 500_000m, annualRate: 5.5m);

        var payoffDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(360);
        var request = new PaymentScheduleRequest(payoffDate, FeeAmount: 0m);

        var sw = Stopwatch.StartNew();
        var response = await _client.PostAsJsonAsync("/api/calculator/schedule", request);
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sw.ElapsedMilliseconds.Should().BeLessThan(500,
            "the 360-period schedule must compute within the p95 < 500ms performance budget");
    }
}
