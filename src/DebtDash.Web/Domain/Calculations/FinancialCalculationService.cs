namespace DebtDash.Web.Domain.Calculations;

/// <summary>
/// One period in a level-payment amortization schedule.
/// </summary>
public record AmortizationPeriod(
    int PeriodNumber,
    DateOnly DueDate,
    decimal Principal,
    decimal Interest,
    decimal RemainingBalance);

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

    /// <summary>
    /// Calculate a level-payment (PMT) amortization schedule.
    /// The base monthly payment is derived with: M = P × [r(1+r)^n] / [(1+r)^n − 1], r = annualRate / 12 / 100.
    /// Per-period interest uses daily accrual: interest = balance × annualRate/100 × daysInMonth/365,
    /// where daysInMonth is the actual number of calendar days in that month.
    /// The final period's principal absorbs rounding so the ending balance is zero (±$0.01).
    /// When annualRate is 0, balance is divided equally across all periods with zero interest.
    /// </summary>
    (decimal MonthlyPayment, IReadOnlyList<AmortizationPeriod> Periods) CalculateMonthlyAmortizationSchedule(
        decimal balance, decimal annualRate, int periods, DateOnly firstDueMonth);
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

    public (decimal MonthlyPayment, IReadOnlyList<AmortizationPeriod> Periods) CalculateMonthlyAmortizationSchedule(
        decimal balance, decimal annualRate, int periods, DateOnly firstDueMonth)
    {
        if (balance <= 0 || periods <= 0)
            return (0m, []);

        var r = annualRate / 12m / 100m;

        decimal monthlyPayment;
        if (r == 0m)
        {
            monthlyPayment = Math.Round(balance / periods, 2);
        }
        else
        {
            var factor = (decimal)Math.Pow((double)(1m + r), periods);
            monthlyPayment = Math.Round(balance * r * factor / (factor - 1m), 2);
        }

        var result = new List<AmortizationPeriod>(periods);
        var remaining = balance;

        for (var i = 1; i <= periods; i++)
        {
            var dueDate = firstDueMonth.AddMonths(i - 1);
            var daysInMonth = dueDate.AddMonths(1).DayNumber - dueDate.DayNumber;
            var interest = Math.Round(remaining * annualRate / 100m * daysInMonth / 365m, 2);

            decimal principal;
            decimal newRemaining;

            if (i == periods)
            {
                // Final period: pay exact remaining balance; absorbs rounding residual.
                principal = remaining;
                newRemaining = 0m;
            }
            else
            {
                principal = Math.Round(monthlyPayment - interest, 2);
                if (principal < 0m) principal = 0m;
                newRemaining = Math.Round(remaining - principal, 2);
            }

            result.Add(new AmortizationPeriod(i, dueDate, principal, interest, newRemaining));
            remaining = newRemaining;
        }

        return (monthlyPayment, result);
    }
}

