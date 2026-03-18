namespace Application.Helpers;

public static class CalculateHelper
{
    public static int CalculateSavingPercentage(decimal pricePerMealStandard, decimal pricePerMealPackage)
    {
        decimal result = (pricePerMealStandard - pricePerMealPackage) / pricePerMealStandard * 100;

        return RoundDecimalToInt(result);
    }

    public static decimal CalculateProductPrice(double discountPercent, decimal price)
    {
        return CustomDecimalRound(price - ((decimal)discountPercent * price / 100));
    }

    public static int RoundDecimalToInt(decimal value)
    {
        int integerPart = (int)value;
        decimal fractionalPart = value - integerPart;

        return fractionalPart switch
        {
            >= 0.1m and <= 0.5m => integerPart,
            >= 0.6m and <= 0.9m => integerPart + 1,
            _ => integerPart + (fractionalPart >= 0.5m ? 1 : 0)
        };
    }

    public static decimal CustomDecimalRound(decimal value)
    {
        decimal tempValue = value * 100;

        decimal wholePart = Math.Floor(tempValue);
        decimal remainder = tempValue - wholePart;

        if (remainder >= 0.6m)
        {
            wholePart += 1;
        }

        return wholePart / 100;
    }
}