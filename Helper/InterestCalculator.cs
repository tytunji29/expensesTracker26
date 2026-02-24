public static class InterestCalculator
{
    public static (decimal, decimal) CalculateSpecialInterestWithReinvestment(
    decimal principal,
    DateTime startDate,
    DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be greater than start date.");

        var totalDays = (decimal)(endDate - startDate).TotalDays;

        if (totalDays <= 0)
            throw new ArgumentException("Invalid duration.");

        var totalMonths = totalDays / 30m;
        var monthlyRate = GetMonthlyRate(totalDays);

        if (monthlyRate == 0m)
            return (0m, 0m);
        decimal totalAmountInvested = principal;
        decimal currentPrincipal = principal;

        while (true)
        {
            var interest = currentPrincipal * (monthlyRate / 100m) * totalMonths;

            if (interest < 10_000m)
            {
                currentPrincipal = interest;
                break;
            }

            totalAmountInvested += interest;
            currentPrincipal = interest;
        }

        return (Math.Round(totalAmountInvested, 2), Math.Round(currentPrincipal, 2));
    }

    private static decimal GetMonthlyRate(decimal totalDays)
    {
        if (totalDays <= 30)
            return 1.2375m;

        if (totalDays <= 60)
            return 1.35m;

        if (totalDays <= 90)
            return 1.50m;

        if (totalDays <= 120)
            return 1.575m;

        if (totalDays <= 180)
            return 1.65m;

        if (totalDays <= 270)
            return 1.725m;

        if (totalDays <= 365)
            return 1.80m;

        return 0m;
    }
}