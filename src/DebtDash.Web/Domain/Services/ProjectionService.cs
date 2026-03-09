using DebtDash.Web.Domain.Models;

namespace DebtDash.Web.Domain.Services;

public interface IProjectionService
{
    ProjectionSnapshot CalculateProjection(LoanProfile loan, List<PaymentLogEntry> payments);
}

public class ProjectionService(ILogger<ProjectionService> logger) : IProjectionService
{
    public ProjectionSnapshot CalculateProjection(LoanProfile loan, List<PaymentLogEntry> payments)
    {
        var ordered = payments.OrderBy(p => p.PaymentDate).ToList();
        var baselineMonthlyPrincipal = loan.InitialPrincipal / loan.TermMonths;
        var baselineRemainingMonths = (decimal)loan.TermMonths;

        if (ordered.Count == 0)
        {
            var baselineEndDate = loan.StartDate.AddMonths(loan.TermMonths);
            return new ProjectionSnapshot
            {
                Id = Guid.NewGuid(),
                LoanProfileId = loan.Id,
                PredictedEndDate = baselineEndDate,
                RemainingMonthsEstimate = baselineRemainingMonths,
                PrincipalVelocity = baselineMonthlyPrincipal,
                BaselineRemainingMonths = baselineRemainingMonths,
                DeltaMonthsVsBaseline = 0m,
                CreatedAt = DateTime.UtcNow
            };
        }

        var lastPayment = ordered[^1];
        var currentBalance = lastPayment.RemainingBalanceAfterPayment;

        // Calculate principal velocity: average principal per month based on recent payments
        var totalPrincipalPaid = ordered.Sum(p => p.PrincipalPaid);
        var firstPaymentDate = ordered[0].PaymentDate;
        var lastPaymentDate = lastPayment.PaymentDate;
        var totalDays = lastPaymentDate.DayNumber - firstPaymentDate.DayNumber;

        decimal principalVelocity;
        if (totalDays > 0 && ordered.Count > 1)
        {
            var monthsElapsed = totalDays / 30.44m;
            principalVelocity = totalPrincipalPaid / monthsElapsed;
        }
        else
        {
            principalVelocity = totalPrincipalPaid > 0 ? totalPrincipalPaid : baselineMonthlyPrincipal;
        }

        // Project remaining months based on velocity
        var remainingMonths = principalVelocity > 0
            ? Math.Round(currentBalance / principalVelocity, 2)
            : baselineRemainingMonths;

        var predictedEndDate = lastPaymentDate.AddDays((int)(remainingMonths * 30.44m));

        // Calculate how many baseline months remain from now
        var monthsSinceStart = (lastPaymentDate.DayNumber - loan.StartDate.DayNumber) / 30.44m;
        var baselineRemaining = Math.Max(0, loan.TermMonths - monthsSinceStart);
        var delta = remainingMonths - baselineRemaining;

        logger.LogInformation(
            "Projection calculated: velocity={Velocity:F2}/mo, remaining={Remaining:F1}mo, delta={Delta:F1}mo",
            principalVelocity, remainingMonths, delta);

        return new ProjectionSnapshot
        {
            Id = Guid.NewGuid(),
            LoanProfileId = loan.Id,
            AsOfPaymentLogEntryId = lastPayment.Id,
            PredictedEndDate = predictedEndDate,
            RemainingMonthsEstimate = remainingMonths,
            PrincipalVelocity = Math.Round(principalVelocity, 4),
            BaselineRemainingMonths = Math.Round(baselineRemaining, 2),
            DeltaMonthsVsBaseline = Math.Round(delta, 2),
            CreatedAt = DateTime.UtcNow
        };
    }
}
