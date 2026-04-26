using Domain.Common.Enums;

namespace Application.Catalog.Pricing.Common;

internal static class MaintainCostCalculator
{
    public static double CalculateMaterialCostPerMetre(
        double partCost,
        double quantity,
        decimal replacementTimeStandard,
        decimal averageMonthlyTunnelProduction,
        MaintainUnitPriceType maintainUnitPriceType)
    {
        var replacementTime = (double)replacementTimeStandard;
        var averageProduction = (double)averageMonthlyTunnelProduction;

        if (replacementTime == 0 || averageProduction == 0)
        {
            return 0;
        }

        var baseMaterialCostPerMetre = partCost * (quantity / (replacementTime * averageProduction));

        return maintainUnitPriceType == MaintainUnitPriceType.Longwall
            ? baseMaterialCostPerMetre / 1000d
            : baseMaterialCostPerMetre;
    }
}