using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Pricing.EletricityUnitPrice;

/// <summary>
/// Công th?c: 
/// - SPðm = Quantity * Pðm
/// - Ptt = SPðm * Kyc * Kðt
/// - PowerConsumptionPerTon = Ptt * WorkingHour * WorkingDate / AverageMonthlyTunnelProduction
/// - EnergyCostPerTon = PowerConsumptionPerTon * Equipment.Cost (Cost t?i th?i ði?m startMonth)
/// </summary>
public class LongwallElectricityUnitPriceEquipment : ElectricityUnitPriceEquipment
{
    public decimal Quantity { get; protected set; }
    public decimal Pdm { get; protected set; }  // Pðm - Công su?t ð?nh m?c
    public double Kyc { get; protected set; }   // H? s? yêu c?u
    public double Kdt { get; protected set; }   // H? s? ð?ng th?i
    public double WorkingHour { get; protected set; }  // Gi? làm vi?c
    public decimal WorkingDate { get; protected set; } // Ngày làm vi?c
    public decimal AverageMonthlyTunnelProduction { get; protected set; }  // S?n lý?ng l? trung b?nh tháng

    // Calculated properties
    public decimal SPdm => Quantity * Pdm;  // T?ng công su?t ð?nh m?c
    public double Ptt => (double)SPdm * Kyc * Kdt;  // Công su?t th?c t?

    //Constructor
    public static LongwallElectricityUnitPriceEquipment Create(
        Guid equipmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        decimal quantity,
        decimal pdm,
        double kyc,
        double kdt,
        double workingHour,
        decimal workingDate,
        decimal averageMonthlyTunnelProduction)
    {
        if (quantity < 0)
        {
            throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
        }

        if (pdm < 0)
        {
            throw new ArgumentException(CustomResponseMessage.PdmCannotBeNegative);
        }

        if (kyc < 0 || kyc > 1)
        {
            throw new ArgumentException(CustomResponseMessage.KycMustBeBetween0And1);
        }

        if (kdt < 0 || kdt > 1)
        {
            throw new ArgumentException(CustomResponseMessage.KdtMustBeBetween0And1);
        }

        if (workingHour < 0)
        {
            throw new ArgumentException(CustomResponseMessage.WorkingHourCannotBeNegative);
        }

        if (workingDate < 0)
        {
            throw new ArgumentException(CustomResponseMessage.WorkingDateCannotBeNegative);
        }

        if (averageMonthlyTunnelProduction < 0)
        {
            throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
        }

        var entity = new LongwallElectricityUnitPriceEquipment
        {
            EquipmentId = equipmentId,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            Quantity = quantity,
            Pdm = pdm,
            Kyc = kyc,
            Kdt = kdt,
            WorkingHour = workingHour,
            WorkingDate = workingDate,
            AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction,
            ElectricityType = ElectricityUnitPriceType.Longwall
        };
        entity.ValidateDateRange(entity.StartMonth, entity.EndMonth);
        return entity;
    }

    /// <summary>
    /// PowerConsumptionPerTon = Ptt * WorkingHour * WorkingDate / AverageMonthlyTunnelProduction
    /// </summary>
    public override double GetElectricityConsumePerMetres()
    {
        if (AverageMonthlyTunnelProduction == 0)
        {
            return 0;
        }

        return (Ptt * WorkingHour * (double)WorkingDate) / (1000 * (double)AverageMonthlyTunnelProduction);
    }

    /// <summary>
    /// EnergyCostPerTon = PowerConsumptionPerTon * Equipment.Cost
    /// </summary>
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
        DateOnly startMonth,
        DateOnly endMonth,
        decimal quantity,
        decimal pdm,
        double kyc,
        double kdt,
        double workingHour,
        decimal workingDate,
        decimal averageMonthlyTunnelProduction)
    {
        if (quantity < 0)
        {
            throw new ArgumentException(CustomResponseMessage.QuantityCannotBeNegative);
        }

        if (pdm < 0)
        {
            throw new ArgumentException(CustomResponseMessage.PdmCannotBeNegative);
        }

        if (kyc < 0 || kyc > 1)
        {
            throw new ArgumentException(CustomResponseMessage.KycMustBeBetween0And1);
        }

        if (kdt < 0 || kdt > 1)
        {
            throw new ArgumentException(CustomResponseMessage.KdtMustBeBetween0And1);
        }

        if (workingHour < 0)
        {
            throw new ArgumentException(CustomResponseMessage.WorkingHourCannotBeNegative);
        }

        if (workingDate < 0)
        {
            throw new ArgumentException(CustomResponseMessage.WorkingDateCannotBeNegative);
        }

        if (averageMonthlyTunnelProduction < 0)
        {
            throw new ArgumentException(CustomResponseMessage.AverageMonthlyTunnelProductionCannotBeNegative);
        }

        ValidateDateRange(startMonth, endMonth);

        EquipmentId = equipmentId;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        Quantity = quantity;
        Pdm = pdm;
        Kyc = kyc;
        Kdt = kdt;
        WorkingHour = workingHour;
        WorkingDate = workingDate;
        AverageMonthlyTunnelProduction = averageMonthlyTunnelProduction;
        CachedTotal = null;
    }
}
