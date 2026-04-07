using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Pricing.EletricityUnitPrice;

/// <summary>
/// Ðào l? - Tunnel Excavation ElectricityUnitPriceEquipment
/// Công th?c: ElectricityCostPerMetres = (MonthlyElectricityCost / AverageMonthlyTunnelProduction) * Equipment.Cost
/// </summary>
public class TunnelElectricityUnitPriceEquipment : ElectricityUnitPriceEquipment
{
    public double MonthlyElectricityCost { get; protected set; }
    public decimal AverageMonthlyTunnelProduction { get; protected set; }

    //Constructor
    public static TunnelElectricityUnitPriceEquipment Create(
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

        var entity = new TunnelElectricityUnitPriceEquipment
        {
            EquipmentId = equipmentId,
            MonthlyElectricityCost = monthlyElectricityCost,
            AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            ElectricityType = ElectricityUnitPriceType.TunnelExcavation
        };
        entity.ValidateDateRange(entity.StartMonth, entity.EndMonth);
        return entity;
    }

    public override double GetElectricityConsumePerMetres()
    {
        if (AverageMonthlyTunnelProduction == 0)
        {
            return 0;
        }

        return (MonthlyElectricityCost / (double)AverageMonthlyTunnelProduction);
    }

    public override double GetElectricityCostPerMetres()
    {
        if (CachedTotal.HasValue)
        {
            return CachedTotal.Value;
        }

        CachedTotal = GetElectricityConsumePerMetres() * GetCurrentElectricityCost();
        return CachedTotal.Value;
    }

    public void Update(
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

        ValidateDateRange(startMonth, endMonth);

        EquipmentId = equipmentId;
        MonthlyElectricityCost = monthlyElectricityCost;
        AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        CachedTotal = null;
    }
}
