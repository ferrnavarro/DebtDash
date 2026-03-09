using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Services;
using DebtDash.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Api;

public static class ProjectionEndpoints
{
    public static RouteGroupBuilder MapProjectionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/true-end-date", async (
            DebtDashDbContext db,
            IProjectionService projectionService) =>
        {
            var loan = await db.LoanProfiles.FirstOrDefaultAsync();
            if (loan is null)
                return Results.NotFound();

            var payments = await db.PaymentLogEntries
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();

            var snapshot = projectionService.CalculateProjection(loan, payments);

            return Results.Ok(new ProjectionSnapshotResponse(
                snapshot.PredictedEndDate,
                snapshot.RemainingMonthsEstimate,
                snapshot.PrincipalVelocity,
                snapshot.BaselineRemainingMonths,
                snapshot.DeltaMonthsVsBaseline));
        });

        return group;
    }
}
