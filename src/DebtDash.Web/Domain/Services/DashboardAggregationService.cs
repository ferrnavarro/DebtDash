using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Models;

namespace DebtDash.Web.Domain.Services;

public interface IDashboardAggregationService
{
    DashboardResponse BuildDashboard(LoanProfile loan, List<PaymentLogEntry> payments);

    /// <summary>
    /// Builds the enriched comparison dashboard response for the requested window.
    /// </summary>
    DashboardComparisonResponse BuildComparisonDashboard(
        LoanProfile loan,
        List<PaymentLogEntry> payments,
        DashboardWindowKey windowKey);
}

public class DashboardAggregationService(
    ILogger<DashboardAggregationService> logger,
    IComparisonTimelineCalculator comparisonCalc) : IDashboardAggregationService
{
    // Minimum payments before a meaningful comparison can be derived
    private const int MinPaymentsForComparison = 2;

    public DashboardResponse BuildDashboard(LoanProfile loan, List<PaymentLogEntry> payments)
    {
        var ordered = payments.OrderBy(p => p.PaymentDate).ToList();

        var totalInterest = ordered.Sum(p => p.InterestPaid);
        var totalCapital = ordered.Sum(p => p.PrincipalPaid);

        // Weighted average real rate: weight by remaining balance at time of payment
        decimal weightedRateSum = 0;
        decimal weightSum = 0;
        foreach (var p in ordered)
        {
            var weight = p.RemainingBalanceAfterPayment + p.PrincipalPaid;
            weightedRateSum += p.CalculatedRealRate * weight;
            weightSum += weight;
        }
        var averageRate = weightSum > 0 ? Math.Round(weightedRateSum / weightSum, 6) : 0m;

        // Time remaining based on last balance and original term
        var lastBalance = ordered.Count > 0 ? ordered[^1].RemainingBalanceAfterPayment : loan.InitialPrincipal;
        var percentRemaining = loan.InitialPrincipal > 0 ? lastBalance / loan.InitialPrincipal : 1m;
        var timeRemainingMonths = Math.Round(percentRemaining * loan.TermMonths, 2);

        var trendSeries = ordered.Select(p => new PrincipalInterestTrendPoint(
                p.PaymentDate, p.PrincipalPaid, p.InterestPaid))
            .ToList();

        var countdownSeries = ordered.Select(p => new DebtCountdownPoint(
                p.PaymentDate, p.RemainingBalanceAfterPayment))
            .ToList();

        logger.LogInformation(
            "Dashboard built: totalInterest={Interest:F2}, totalCapital={Capital:F2}, avgRate={Rate:F4}%",
            totalInterest, totalCapital, averageRate);

        return new DashboardResponse(
            Math.Round(totalInterest, 2),
            Math.Round(totalCapital, 2),
            averageRate,
            timeRemainingMonths,
            loan.TermMonths,
            trendSeries,
            countdownSeries);
    }

    public DashboardComparisonResponse BuildComparisonDashboard(
        LoanProfile loan,
        List<PaymentLogEntry> payments,
        DashboardWindowKey windowKey)
    {
        var ordered = payments.OrderBy(p => p.PaymentDate).ToList();
        var availableWindows = BuildAvailableWindows(loan, ordered);
        var activeWindow = availableWindows.First(w => w.Key == windowKey);

        if (ordered.Count == 0)
        {
            logger.LogInformation(
                "Comparison dashboard: no payments, returning empty state. LoanId={LoanId}",
                loan.Id);

            return BuildEmptyResponse(availableWindows, activeWindow);
        }

        if (ordered.Count < MinPaymentsForComparison)
        {
            logger.LogInformation(
                "Comparison dashboard: insufficient data ({Count} payments). LoanId={LoanId}",
                ordered.Count, loan.Id);

            return BuildLimitedDataResponse(loan, ordered, availableWindows, activeWindow, windowKey);
        }

        var balanceSeries = comparisonCalc.BuildTimeline(loan, ordered, activeWindow.RangeStart, activeWindow.RangeEnd);
        var costSeries = comparisonCalc.BuildTimeline(loan, ordered, activeWindow.RangeStart, activeWindow.RangeEnd);

        var summary = DeriveSummary(loan, ordered, balanceSeries, windowKey);
        var milestones = DeriveMilestones(balanceSeries, ordered);

        logger.LogInformation(
            "Comparison dashboard built: window={Window}, status={Status}, points={Points}, milestones={Milestones}. LoanId={LoanId}",
            windowKey, summary.CurrentStatus, balanceSeries.Count, milestones.Count, loan.Id);

        return new DashboardComparisonResponse(
            summary,
            balanceSeries,
            costSeries,
            milestones,
            availableWindows,
            activeWindow,
            DashboardState.Ready);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Window building
    // ──────────────────────────────────────────────────────────────────────────

    private static List<DashboardWindowResponse> BuildAvailableWindows(
        LoanProfile loan, List<PaymentLogEntry> ordered)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var earliest = ordered.Count > 0 ? ordered[0].PaymentDate : loan.StartDate;
        var latest = ordered.Count > 0 ? ordered[^1].PaymentDate : today;
        // Full-history rangeEnd caps at the last payment date so the comparison
        // only spans periods where we have actual data, avoiding misleading signals
        // from months where no payments were recorded.
        var fullHistoryEnd = latest;

        return
        [
            new DashboardWindowResponse(
                DashboardWindowKey.FullHistory,
                "Full History",
                earliest,
                fullHistoryEnd),
            new DashboardWindowResponse(
                DashboardWindowKey.Trailing6Months,
                "Last 6 Months",
                today.AddMonths(-6),
                today),
            new DashboardWindowResponse(
                DashboardWindowKey.Trailing12Months,
                "Last 12 Months",
                today.AddMonths(-12),
                today),
            new DashboardWindowResponse(
                DashboardWindowKey.YearToDate,
                "Year to Date",
                new DateOnly(today.Year, 1, 1),
                today),
        ];
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Summary derivation (T016 / US1)
    // ──────────────────────────────────────────────────────────────────────────

    private ComparisonSummaryResponse DeriveSummary(
        LoanProfile loan,
        List<PaymentLogEntry> ordered,
        List<ComparisonTimelinePointResponse> timeline,
        DashboardWindowKey windowKey)
    {
        if (timeline.Count == 0)
        {
            return new ComparisonSummaryResponse(
                windowKey,
                ComparisonStatus.InsufficientData,
                null, null, null, null, null,
                DateTime.UtcNow,
                "No data available for the selected window.");
        }

        var latest = timeline[^1];
        var balanceDelta = latest.BalanceDelta; // positive = ahead (lower actual balance)
        var interestAvoided = latest.InterestDelta; // positive = ahead

        // Determine status: if actual balance is substantially below baseline → ahead
        var status = DetermineStatus(balanceDelta, interestAvoided);

        // Months saved: use payoff delta from most recent timeline point
        var monthsSaved = latest.PayoffProgressDeltaMonths > 0 ? latest.PayoffProgressDeltaMonths : (decimal?)null;

        // First divergence date: first point where extra principal took effect
        DateOnly? firstDivergence = timeline
            .FirstOrDefault(p => p.ContainsExtraPrincipalEffect)?.Date;

        // Projected payoff delta: positive = earlier payoff
        decimal? projectedDelta = latest.PayoffProgressDeltaMonths != 0 ? latest.PayoffProgressDeltaMonths : null;

        // Remaining balance delta: positive = actual is lower
        var remainingDelta = balanceDelta != 0 ? balanceDelta : (decimal?)null;

        var message = BuildStateMessage(status, monthsSaved, interestAvoided, firstDivergence);

        return new ComparisonSummaryResponse(
            windowKey,
            status,
            monthsSaved.HasValue ? Math.Round(monthsSaved.Value, 1) : null,
            projectedDelta.HasValue ? Math.Round(projectedDelta.Value, 1) : null,
            remainingDelta.HasValue ? Math.Round(remainingDelta.Value, 2) : null,
            interestAvoided > 0.01m ? Math.Round(interestAvoided, 2) : null,
            firstDivergence,
            DateTime.UtcNow,
            message);
    }

    private static ComparisonStatus DetermineStatus(decimal balanceDelta, decimal interestDelta)
    {
        if (balanceDelta > 1m || interestDelta > 1m)
            return ComparisonStatus.Ahead;
        if (balanceDelta < -1m || interestDelta < -1m)
            return ComparisonStatus.Behind;
        return ComparisonStatus.OnTrack;
    }

    private static string BuildStateMessage(
        ComparisonStatus status,
        decimal? monthsSaved,
        decimal interestAvoided,
        DateOnly? firstDivergence)
    {
        return status switch
        {
            ComparisonStatus.Ahead when monthsSaved.HasValue =>
                $"You are ahead of your original schedule by approximately {monthsSaved:F1} month(s), " +
                $"saving ${interestAvoided:F2} in interest so far.",
            ComparisonStatus.Ahead =>
                $"You are ahead of your original schedule, saving ${interestAvoided:F2} in interest so far.",
            ComparisonStatus.Behind =>
                "Your actual payments are behind the original payment schedule. " +
                "Consider adding extra principal to get back on track.",
            ComparisonStatus.OnTrack =>
                firstDivergence.HasValue
                    ? $"You are exactly on track with your original schedule since {firstDivergence}."
                    : "You are on track with your original payment schedule.",
            ComparisonStatus.InsufficientData =>
                "Not enough payment history is available to compare against the baseline schedule.",
            _ => string.Empty,
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Milestone derivation (T036 / US3)
    // ──────────────────────────────────────────────────────────────────────────

    private static List<ComparisonMilestoneResponse> DeriveMilestones(
        List<ComparisonTimelinePointResponse> timeline,
        List<PaymentLogEntry> ordered)
    {
        var milestones = new List<ComparisonMilestoneResponse>();

        if (timeline.Count == 0)
            return milestones;

        // Divergence start: first point where extra principal effect is present
        var divergenceStart = timeline.FirstOrDefault(p => p.ContainsExtraPrincipalEffect);
        if (divergenceStart is not null)
        {
            milestones.Add(new ComparisonMilestoneResponse(
                MilestoneType.DivergenceStart,
                divergenceStart.Date,
                "Extra Principal Journey Began",
                "This is the first payment where your principal paid exceeded the baseline schedule.",
                divergenceStart.BalanceDelta));
        }

        // Highest balance gap: point where balanceDelta is maximum
        var maxGap = timeline.MaxBy(p => p.BalanceDelta);
        if (maxGap is not null && maxGap.BalanceDelta > 1m)
        {
            milestones.Add(new ComparisonMilestoneResponse(
                MilestoneType.HighestBalanceGap,
                maxGap.Date,
                "Largest Balance Advantage",
                $"Your balance was ${maxGap.BalanceDelta:F2} lower than the baseline schedule at its peak.",
                maxGap.BalanceDelta));
        }

        // Highest interest savings: point where interestDelta is maximum
        var maxInterestSavings = timeline.MaxBy(p => p.InterestDelta);
        if (maxInterestSavings is not null && maxInterestSavings.InterestDelta > 1m)
        {
            milestones.Add(new ComparisonMilestoneResponse(
                MilestoneType.HighestInterestSavings,
                maxInterestSavings.Date,
                "Greatest Interest Savings",
                $"You had saved ${maxInterestSavings.InterestDelta:F2} in cumulative interest versus the baseline.",
                maxInterestSavings.InterestDelta));
        }

        // Early payoff indicator: if the last actual balance is 0 but baseline isn't
        var last = timeline[^1];
        if (last.ActualRemainingBalance <= 0 && last.BaselineRemainingBalance > 0)
        {
            milestones.Add(new ComparisonMilestoneResponse(
                MilestoneType.EarlyPayoff,
                last.Date,
                "Early Payoff Achieved",
                $"Loan paid off {last.PayoffProgressDeltaMonths:F1} month(s) ahead of schedule.",
                last.PayoffProgressDeltaMonths));
        }

        return milestones.OrderBy(m => m.Date).ToList();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Empty / limited-data responses (T012 / T020)
    // ──────────────────────────────────────────────────────────────────────────

    private static DashboardComparisonResponse BuildEmptyResponse(
        List<DashboardWindowResponse> availableWindows,
        DashboardWindowResponse activeWindow)
    {
        var emptySummary = new ComparisonSummaryResponse(
            activeWindow.Key,
            ComparisonStatus.InsufficientData,
            null, null, null, null, null,
            DateTime.UtcNow,
            "Add payments to start comparing your actual progress against the original loan schedule.");

        return new DashboardComparisonResponse(
            emptySummary, [], [], [], availableWindows, activeWindow, DashboardState.Empty);
    }

    private DashboardComparisonResponse BuildLimitedDataResponse(
        LoanProfile loan,
        List<PaymentLogEntry> ordered,
        List<DashboardWindowResponse> availableWindows,
        DashboardWindowResponse activeWindow,
        DashboardWindowKey windowKey)
    {
        var message = $"Only {ordered.Count} payment(s) recorded. " +
                      $"At least {MinPaymentsForComparison} payments are needed for a meaningful comparison.";

        logger.LogInformation(
            "Limited-data comparison response: {Message} LoanId={LoanId}", message, loan.Id);

        var limitedSummary = new ComparisonSummaryResponse(
            windowKey,
            ComparisonStatus.InsufficientData,
            null, null, null, null, null,
            DateTime.UtcNow,
            message);

        return new DashboardComparisonResponse(
            limitedSummary, [], [], [], availableWindows, activeWindow, DashboardState.LimitedData);
    }
}
