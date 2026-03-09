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
            IDashboardAggregationService dashboardService,
            string? window) =>
        {
            var loan = await db.LoanProfiles.FirstOrDefaultAsync();
            if (loan is null)
                return Results.NotFound();

            var payments = await db.PaymentLogEntries
                .Include(p => p.RateVariance)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync();

            var windowKey = ParseWindowKey(window);
            return Results.Ok(dashboardService.BuildComparisonDashboard(loan, payments, windowKey));
        });

        return group;
    }

    private static DashboardWindowKey ParseWindowKey(string? window) =>
        window?.ToLowerInvariant() switch
        {
            "trailing-6-months" => DashboardWindowKey.Trailing6Months,
            "trailing-12-months" => DashboardWindowKey.Trailing12Months,
            "year-to-date" => DashboardWindowKey.YearToDate,
            _ => DashboardWindowKey.FullHistory, // default and "full-history"
        };
}
