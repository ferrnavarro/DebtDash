using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Models;

namespace DebtDash.Web.Domain.Calculations;

/// <summary>
/// T008: Derives the no-extra-principal baseline amortization path and compares it
/// against the actual payment history to produce <see cref="ComparisonTimelinePointResponse"/>
/// sequences used by charts and summary derivation.
/// </summary>
public interface IComparisonTimelineCalculator
{
    /// <summary>
    /// Builds a comparison timeline from earliest payment date to today (or last payment),
    /// filtered to the given window range.
    /// </summary>
    List<ComparisonTimelinePointResponse> BuildTimeline(
        LoanProfile loan,
        List<PaymentLogEntry> actualPayments,
        DateOnly windowStart,
        DateOnly windowEnd);

    /// <summary>
    /// Derives what the baseline (no-extra-principal) schedule would look like for the
    /// requested window, independent of actual payments.
    /// </summary>
    List<ComparisonTimelinePointResponse> BuildBaselineTimeline(
        LoanProfile loan,
        DateOnly windowStart,
        DateOnly windowEnd);
}

public class ComparisonTimelineCalculator(IFinancialCalculationService calc) : IComparisonTimelineCalculator
{
    private const int DaysPerYear = 365;
    private const decimal MinMeaningfulBalanceDelta = 1m; // $1 threshold for divergence detection

    public List<ComparisonTimelinePointResponse> BuildTimeline(
        LoanProfile loan,
        List<PaymentLogEntry> actualPayments,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        if (actualPayments.Count == 0)
            return [];

        var ordered = actualPayments.OrderBy(p => p.PaymentDate).ToList();

        // Build baseline schedule month-by-month from loan start date
        var baselineSchedule = BuildMonthlyBaselineSchedule(loan);

        // Build actual cumulative series
        var actualSeries = BuildActualCumulativeSeries(loan, ordered);

        // Merge into timeline points within the window
        var points = new List<ComparisonTimelinePointResponse>();

        // Union of dates from actual and baseline within window
        var allDates = actualSeries.Keys
            .Union(baselineSchedule.Keys)
            .Where(d => d >= windowStart && d <= windowEnd)
            .OrderBy(d => d)
            .ToList();

        foreach (var date in allDates)
        {
            var actual = GetOrInterpolateActual(actualSeries, date, loan.InitialPrincipal);
            var baseline = GetOrInterpolateBaseline(baselineSchedule, date, loan.InitialPrincipal);

            var balanceDelta = baseline.RemainingBalance - actual.RemainingBalance;
            var interestDelta = baseline.CumulativeInterest - actual.CumulativeInterest;

            // Months ahead: positive = ahead of baseline (lower balance → ahead)
            var payoffProgressDeltaMonths = loan.AnnualRate > 0
                ? Math.Round(balanceDelta / (loan.InitialPrincipal / loan.TermMonths), 2)
                : 0m;

            // Extra principal effect: actual paid more principal than baseline at this point
            var containsExtraEffect = actual.CumulativePrincipal > baseline.CumulativePrincipal + MinMeaningfulBalanceDelta;

            points.Add(new ComparisonTimelinePointResponse(
                date,
                Math.Round(actual.RemainingBalance, 2),
                Math.Round(baseline.RemainingBalance, 2),
                Math.Round(actual.CumulativeInterest, 2),
                Math.Round(baseline.CumulativeInterest, 2),
                Math.Round(actual.CumulativePrincipal, 2),
                Math.Round(baseline.CumulativePrincipal, 2),
                Math.Round(balanceDelta, 2),
                Math.Round(interestDelta, 2),
                payoffProgressDeltaMonths,
                containsExtraEffect));
        }

        return points;
    }

    public List<ComparisonTimelinePointResponse> BuildBaselineTimeline(
        LoanProfile loan,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        var schedule = BuildMonthlyBaselineSchedule(loan);
        var points = new List<ComparisonTimelinePointResponse>();

        foreach (var (date, snap) in schedule.Where(kv => kv.Key >= windowStart && kv.Key <= windowEnd).OrderBy(kv => kv.Key))
        {
            points.Add(new ComparisonTimelinePointResponse(
                date,
                Math.Round(snap.RemainingBalance, 2),
                Math.Round(snap.RemainingBalance, 2),
                Math.Round(snap.CumulativeInterest, 2),
                Math.Round(snap.CumulativeInterest, 2),
                Math.Round(snap.CumulativePrincipal, 2),
                Math.Round(snap.CumulativePrincipal, 2),
                0m, 0m, 0m, false));
        }

        return points;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Internals
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a month-by-month no-extra-principal amortization schedule using
    /// ACT/365 interest accrual (matching the rest of the system).
    /// </summary>
    private Dictionary<DateOnly, BalanceSnapshot> BuildMonthlyBaselineSchedule(LoanProfile loan)
    {
        var result = new Dictionary<DateOnly, BalanceSnapshot>();
        var remainingBalance = loan.InitialPrincipal;
        decimal cumulativeInterest = 0m;
        decimal cumulativePrincipal = 0m;

        // Derive baseline monthly payment using standard annuity formula
        var monthlyRate = loan.AnnualRate / 100m / 12m;
        decimal baselineMonthlyPrincipal;

        if (monthlyRate > 0)
        {
            var pmt = loan.InitialPrincipal * monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), loan.TermMonths)
                      / ((decimal)Math.Pow((double)(1 + monthlyRate), loan.TermMonths) - 1);
            // We'll derive the principal portion each month using actual interest formula
            baselineMonthlyPrincipal = pmt; // used as total payment, not just principal
        }
        else
        {
            baselineMonthlyPrincipal = Math.Round(loan.InitialPrincipal / loan.TermMonths, 2);
        }

        var date = loan.StartDate;
        for (var month = 0; month < loan.TermMonths && remainingBalance > 0; month++)
        {
            date = date.AddMonths(1);
            // ACT/365 interest for approximately 30 days (baseline: treat each month as 30 days)
            var daysInPeriod = 30;
            var interest = calc.CalculateExpectedInterest(remainingBalance, loan.AnnualRate, daysInPeriod);

            decimal principal;
            if (monthlyRate > 0)
            {
                // Total payment = baselineMonthlyPrincipal (the annuity PMT)
                principal = baselineMonthlyPrincipal - interest;
                if (principal < 0) principal = 0m;
            }
            else
            {
                principal = loan.InitialPrincipal / loan.TermMonths;
            }

            principal = Math.Min(principal, remainingBalance);
            remainingBalance = Math.Max(0m, remainingBalance - principal);
            cumulativeInterest += interest;
            cumulativePrincipal += principal;

            result[date] = new BalanceSnapshot(remainingBalance, cumulativeInterest, cumulativePrincipal);
        }

        return result;
    }

    /// <summary>
    /// Builds a cumulative series indexed by payment date from actual payment history.
    /// </summary>
    private static Dictionary<DateOnly, BalanceSnapshot> BuildActualCumulativeSeries(
        LoanProfile loan, List<PaymentLogEntry> ordered)
    {
        var result = new Dictionary<DateOnly, BalanceSnapshot>();
        decimal cumulativeInterest = 0m;
        decimal cumulativePrincipal = 0m;

        foreach (var p in ordered)
        {
            cumulativeInterest += p.InterestPaid;
            cumulativePrincipal += p.PrincipalPaid;
            result[p.PaymentDate] = new BalanceSnapshot(
                p.RemainingBalanceAfterPayment,
                cumulativeInterest,
                cumulativePrincipal);
        }

        return result;
    }

    private static BalanceSnapshot GetOrInterpolateActual(
        Dictionary<DateOnly, BalanceSnapshot> series, DateOnly date, decimal initialPrincipal)
    {
        if (series.TryGetValue(date, out var snap))
            return snap;

        // Use the most recent actual snapshot before this date
        var prev = series.Keys.Where(d => d < date).OrderByDescending(d => d).FirstOrDefault();
        if (prev != default)
            return series[prev];

        return new BalanceSnapshot(initialPrincipal, 0m, 0m);
    }

    private static BalanceSnapshot GetOrInterpolateBaseline(
        Dictionary<DateOnly, BalanceSnapshot> schedule, DateOnly date, decimal initialPrincipal)
    {
        if (schedule.TryGetValue(date, out var snap))
            return snap;

        // Linear interpolation between surrounding months
        var prev = schedule.Keys.Where(d => d <= date).OrderByDescending(d => d).FirstOrDefault();
        var next = schedule.Keys.Where(d => d > date).OrderBy(d => d).FirstOrDefault();

        if (prev == default && next == default)
            return new BalanceSnapshot(initialPrincipal, 0m, 0m);
        if (prev == default)
            return schedule[next];
        if (next == default)
            return schedule[prev];

        var totalDays = next.DayNumber - prev.DayNumber;
        var elapsed = date.DayNumber - prev.DayNumber;
        var t = totalDays > 0 ? (decimal)elapsed / totalDays : 0m;

        var prevSnap = schedule[prev];
        var nextSnap = schedule[next];
        return new BalanceSnapshot(
            prevSnap.RemainingBalance + (nextSnap.RemainingBalance - prevSnap.RemainingBalance) * t,
            prevSnap.CumulativeInterest + (nextSnap.CumulativeInterest - prevSnap.CumulativeInterest) * t,
            prevSnap.CumulativePrincipal + (nextSnap.CumulativePrincipal - prevSnap.CumulativePrincipal) * t);
    }

    private record BalanceSnapshot(decimal RemainingBalance, decimal CumulativeInterest, decimal CumulativePrincipal);
}
