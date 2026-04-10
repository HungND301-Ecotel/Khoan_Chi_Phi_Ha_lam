using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Pricing.EletricityUnitPrice;

public class TrimmingElectricityUnitPriceEquipment : TunnelElectricityUnitPriceEquipment
{
    public static TrimmingElectricityUnitPriceEquipment Create(
        Guid equipmentId,
        double monthlyElectricityCost,
        decimal averageMonthlyTunnelProduction,
        DateOnly startMonth,
        DateOnly endMonth)
    {
        if (monthlyElectricityCost < 0)
        {
            throw new ArgumentException(CustomResponseMessage.MonthlyElectricityCostCannotBeNegative);
        }

        if (averageMonthlyTunnelProduction < 0)
        {
            throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
        }

        var entity = new TrimmingElectricityUnitPriceEquipment
        {
            EquipmentId = equipmentId,
            MonthlyElectricityCost = monthlyElectricityCost,
            AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            ElectricityType = ElectricityUnitPriceType.Trimming
        };

        entity.ValidateDateRange(entity.StartMonth, entity.EndMonth);
        return entity;
    }
}
