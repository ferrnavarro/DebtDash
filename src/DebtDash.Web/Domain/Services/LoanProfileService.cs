using DebtDash.Web.Api.Contracts;
using DebtDash.Web.Domain.Calculations;
using DebtDash.Web.Domain.Models;
using DebtDash.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtDash.Web.Domain.Services;

public interface ILoanProfileService
{
    Task<LoanProfile?> GetAsync();
    Task<LoanProfile> UpsertAsync(LoanProfileUpsertRequest request);
}

public class LoanProfileService(
    DebtDashDbContext db,
    ILogger<LoanProfileService> logger) : ILoanProfileService
{
    public async Task<LoanProfile?> GetAsync()
    {
        return await db.LoanProfiles.FirstOrDefaultAsync();
    }

    public async Task<LoanProfile> UpsertAsync(LoanProfileUpsertRequest request)
    {
        var existing = await db.LoanProfiles.FirstOrDefaultAsync();
        if (existing is null)
        {
            existing = new LoanProfile
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            db.LoanProfiles.Add(existing);
            logger.LogInformation("Creating new loan profile {Id}", existing.Id);
        }
        else
        {
            logger.LogInformation("Updating loan profile {Id}", existing.Id);
        }

        existing.InitialPrincipal = request.InitialPrincipal;
        existing.AnnualRate = request.AnnualRate;
        existing.TermMonths = request.TermMonths;
        existing.StartDate = request.StartDate;
        existing.FixedMonthlyCosts = request.FixedMonthlyCosts;
        existing.CurrencyCode = request.CurrencyCode;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return existing;
    }
}
