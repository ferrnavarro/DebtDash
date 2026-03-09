namespace DebtDash.Web.Domain.Calculations;

public interface IFinancialCalculationService
{
    /// <summary>
    /// Calculate interest for a period based on remaining balance, annual rate, and days elapsed.
    /// </summary>
    decimal CalculateExpectedInterest(decimal remainingBalance, decimal annualRate, int daysElapsed);

    /// <summary>
    /// Calculate the real annual rate from actual interest paid, balance, and days elapsed.
    /// </summary>
    decimal CalculateRealAnnualRate(decimal interestPaid, decimal remainingBalance, int daysElapsed);

    /// <summary>
    /// Calculate days between two dates.
    /// </summary>
    int CalculateDaysElapsed(DateOnly previousDate, DateOnly currentDate);

    /// <summary>
    /// Calculate remaining balance after a payment.
    /// </summary>
    decimal CalculateRemainingBalance(decimal previousBalance, decimal principalPaid);
}

public class FinancialCalculationService : IFinancialCalculationService
{
    private const int DaysPerYear = 365;

    public decimal CalculateExpectedInterest(decimal remainingBalance, decimal annualRate, int daysElapsed)
    {
        if (remainingBalance <= 0 || annualRate <= 0 || daysElapsed <= 0)
            return 0m;

        return Math.Round(remainingBalance * annualRate / 100m * daysElapsed / DaysPerYear, 2);
    }

    public decimal CalculateRealAnnualRate(decimal interestPaid, decimal remainingBalance, int daysElapsed)
    {
        if (remainingBalance <= 0 || daysElapsed <= 0)
            return 0m;

        return Math.Round(interestPaid / remainingBalance * DaysPerYear / daysElapsed * 100m, 6);
    }

    public int CalculateDaysElapsed(DateOnly previousDate, DateOnly currentDate)
    {
        return currentDate.DayNumber - previousDate.DayNumber;
    }

    public decimal CalculateRemainingBalance(decimal previousBalance, decimal principalPaid)
    {
        var result = previousBalance - principalPaid;
        return result < 0 ? 0m : result;
    }
}
