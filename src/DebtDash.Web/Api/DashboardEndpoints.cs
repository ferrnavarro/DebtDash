using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Services;
using DebtDash.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Api;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            DebtDashDbContext db,
            IDashboardAggregationService dashboardService) =>
        {
            var loan = await db.LoanProfiles.FirstOrDefaultAsync();
            if (loan is null)
                return Results.NotFound();

            var payments = await db.PaymentLogEntries
                .Include(p => p.RateVariance)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            return Results.Ok(dashboardService.BuildDashboard(loan, payments));
        });

        return group;
    }
}
