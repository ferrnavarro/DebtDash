using DebtDash.Web.Domain.Models;

namespace DebtDash.Web.UnitTests.Domain;

/// <summary>
/// T003: Shared comparison test data helpers for unit tests.
/// Provides factory methods for common loan and payment scenarios used across
/// DashboardAggregationService and ComparisonTimelineCalculator tests.
/// </summary>
public static class ComparisonTestData
{
    /// <summary>
    /// A standard $200k / 5.5% / 30-year loan starting 2024-01-15.
    /// </summary>
    public static LoanProfile StandardLoan() => new()
    {
        Id = Guid.NewGuid(),
        InitialPrincipal = 200_000m,
        AnnualRate = 5.5m,
        TermMonths = 360,
        StartDate = new DateOnly(2024, 1, 15),
        FixedMonthlyCosts = 50m,
        CurrencyCode = "USD",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    /// <summary>
    /// Six payments at the minimum/baseline amount — no extra principal.
    /// Principal of ~$230/month matches the amortization baseline so the calculator
    /// does not flag any divergence from the no-extra-principal schedule.
    /// </summary>
    public static List<PaymentLogEntry> SixBaselinePayments(Guid loanId)
    {
        var payments = new List<PaymentLogEntry>();
        var balance = 200_000m;
        // Realistic minimum principal for $200k/5.5%/360-mo loan ≈ $230/month
        // (annuity PMT ≈ $1136, interest ≈ $904 in month 1, principal ≈ $232)
        for (var i = 0; i < 6; i++)
        {
            var principal = 230m; // just below baseline minimum → no divergence
            var interest = 904m;
            balance -= principal;
            payments.Add(new PaymentLogEntry
            {
                Id = Guid.NewGuid(),
                LoanProfileId = loanId,
                PaymentDate = new DateOnly(2024, 2 + i, 15),
                TotalPaid = principal + interest + 50m,
                PrincipalPaid = principal,
                InterestPaid = interest,
                FeesPaid = 50m,
                DaysSincePreviousPayment = 30,
                RemainingBalanceAfterPayment = balance,
                CalculatedRealRate = 5.5m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        return payments;
    }

    /// <summary>
    /// Three payments with extra principal followed by three baseline payments.
    /// Interest amounts reflect the actual lower balance after extra principal payments.
    /// </summary>
    public static List<PaymentLogEntry> MixedPayments(Guid loanId)
    {
        // Realistic interest calculation: ACT/365 with 30 days per period
        // Month 1: 200000 * 0.055 * 30/365 = $904  (extra principal)
        // Month 2: 199200 * 0.055 * 30/365 = $900  (extra principal)
        // Month 3: 198400 * 0.055 * 30/365 = $897  (extra principal)
        // Month 4: 197600 * 0.055 * 30/365 = $893  (baseline)
        // Month 5: 197370 * 0.055 * 30/365 = $892  (baseline)
        // Month 6: 197140 * 0.055 * 30/365 = $891  (baseline)
        var entries = new (decimal Principal, decimal Interest)[]
        {
            (800m, 904m),
            (800m, 900m),
            (800m, 897m),
            (230m, 893m),
            (230m, 892m),
            (230m, 891m),
        };

        var payments = new List<PaymentLogEntry>();
        var balance = 200_000m;

        for (var i = 0; i < entries.Length; i++)
        {
            var (principal, interest) = entries[i];
            balance -= principal;
            payments.Add(new PaymentLogEntry
            {
                Id = Guid.NewGuid(),
                LoanProfileId = loanId,
                PaymentDate = new DateOnly(2024, 2 + i, 15),
                TotalPaid = principal + interest + 50m,
                PrincipalPaid = principal,
                InterestPaid = interest,
                FeesPaid = 50m,
                DaysSincePreviousPayment = 30,
                RemainingBalanceAfterPayment = balance,
                CalculatedRealRate = 5.5m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        return payments;
    }

    /// <summary>
    /// A single payment — used to test insufficient-data states.
    /// </summary>
    public static List<PaymentLogEntry> SinglePayment(Guid loanId) =>
    [
        new PaymentLogEntry
        {
            Id = Guid.NewGuid(),
            LoanProfileId = loanId,
            PaymentDate = new DateOnly(2024, 2, 15),
            TotalPaid = 1_300m,
            PrincipalPaid = 800m,
            InterestPaid = 450m,
            FeesPaid = 50m,
            DaysSincePreviousPayment = 31,
            RemainingBalanceAfterPayment = 199_200m,
            CalculatedRealRate = 5.5m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }
    ];

    /// <summary>
    /// No payments — used to test empty/insufficient-data states.
    /// </summary>
    public static List<PaymentLogEntry> NoPayments() => [];
}
