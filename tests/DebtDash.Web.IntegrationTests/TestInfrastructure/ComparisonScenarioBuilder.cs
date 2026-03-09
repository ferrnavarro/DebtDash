using System.Net.Http.Json;

namespace DebtDash.Web.IntegrationTests.TestInfrastructure;

/// <summary>
/// T002: Fluent builder for seeding comparison test scenarios via the HTTP API.
/// Encapsulates the common patterns for loans with/without extra-principal payments.
/// </summary>
public class ComparisonScenarioBuilder
{
    private readonly HttpClient _client;

    private decimal _initialPrincipal = 200_000m;
    private decimal _annualRate = 5.5m;
    private int _termMonths = 360;
    private DateOnly _startDate = new(2024, 1, 15);
    private decimal _fixedMonthlyCosts = 50m;
    private string _currencyCode = "USD";

    private readonly List<PaymentSpec> _payments = [];

    public ComparisonScenarioBuilder(HttpClient client)
    {
        _client = client;
    }

    public ComparisonScenarioBuilder WithLoan(
        decimal initialPrincipal,
        decimal annualRate = 5.5m,
        int termMonths = 360,
        DateOnly? startDate = null,
        decimal fixedMonthlyCosts = 50m,
        string currencyCode = "USD")
    {
        _initialPrincipal = initialPrincipal;
        _annualRate = annualRate;
        _termMonths = termMonths;
        _startDate = startDate ?? new DateOnly(2024, 1, 15);
        _fixedMonthlyCosts = fixedMonthlyCosts;
        _currencyCode = currencyCode;
        return this;
    }

    /// <summary>
    /// Adds a baseline (no-extra-principal) payment — only the minimum amount each month.
    /// </summary>
    public ComparisonScenarioBuilder WithBaselinePayment(DateOnly date, decimal principal, decimal interest, decimal fees = 0m)
    {
        _payments.Add(new PaymentSpec(date, principal, interest, fees, IsExtra: false));
        return this;
    }

    /// <summary>
    /// Adds a payment with extra principal to demonstrate divergence from baseline.
    /// </summary>
    public ComparisonScenarioBuilder WithExtraPayment(DateOnly date, decimal basePrincipal, decimal extraPrincipal, decimal interest, decimal fees = 0m)
    {
        _payments.Add(new PaymentSpec(date, basePrincipal + extraPrincipal, interest, fees, IsExtra: true));
        return this;
    }

    /// <summary>
    /// Seeds 6 monthly baseline-only payments starting from the loan start date + 1 month.
    /// </summary>
    public ComparisonScenarioBuilder WithSixBaselinePayments()
    {
        for (var i = 0; i < 6; i++)
        {
            var date = _startDate.AddMonths(i + 1);
            _payments.Add(new PaymentSpec(date, 800m, 450m, 50m, IsExtra: false));
        }
        return this;
    }

    /// <summary>
    /// Seeds 6 monthly payments where the first three have extra principal to create divergence.
    /// </summary>
    public ComparisonScenarioBuilder WithMixedPayments()
    {
        for (var i = 0; i < 3; i++)
        {
            var date = _startDate.AddMonths(i + 1);
            _payments.Add(new PaymentSpec(date, 1200m, 450m, 50m, IsExtra: true)); // extra principal
        }
        for (var i = 3; i < 6; i++)
        {
            var date = _startDate.AddMonths(i + 1);
            _payments.Add(new PaymentSpec(date, 800m, 440m, 50m, IsExtra: false)); // baseline
        }
        return this;
    }

    /// <summary>
    /// Seeds the loan and all configured payments, returning once all HTTP calls succeed.
    /// </summary>
    public async Task BuildAsync()
    {
        var loanResponse = await _client.PutAsJsonAsync("/api/loan", new
        {
            initialPrincipal = _initialPrincipal,
            annualRate = _annualRate,
            termMonths = _termMonths,
            startDate = _startDate.ToString("yyyy-MM-dd"),
            fixedMonthlyCosts = _fixedMonthlyCosts,
            currencyCode = _currencyCode,
        });
        loanResponse.EnsureSuccessStatusCode();

        foreach (var p in _payments.OrderBy(p => p.Date))
        {
            var paymentResponse = await _client.PostAsJsonAsync("/api/payments", new
            {
                paymentDate = p.Date.ToString("yyyy-MM-dd"),
                totalPaid = p.Principal + p.Interest + p.Fees,
                principalPaid = p.Principal,
                interestPaid = p.Interest,
                feesPaid = p.Fees,
                manualRateOverrideEnabled = false,
                manualRateOverride = (decimal?)null,
            });
            paymentResponse.EnsureSuccessStatusCode();
        }
    }

    private record PaymentSpec(DateOnly Date, decimal Principal, decimal Interest, decimal Fees, bool IsExtra);
}
