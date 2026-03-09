using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Models;

namespace DebtDash.Web.Domain.Services;

public interface IDashboardAggregationService
{
    DashboardResponse BuildDashboard(LoanProfile loan, List<PaymentLogEntry> payments);
}

public class DashboardAggregationService(ILogger<DashboardAggregationService> logger) : IDashboardAggregationService
{
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
}
