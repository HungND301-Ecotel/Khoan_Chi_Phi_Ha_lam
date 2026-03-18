using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing.EletricityUnitPrice;

public abstract class ElectricityUnitPriceEquipment : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid EquipmentId { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public ElectricityUnitPriceType ElectricityType { get; protected set; } = ElectricityUnitPriceType.TunnelExcavation;

    protected double? CachedTotal { get; set; }

    //Navigation Properties
    public virtual Equipment? Equipment { get; protected set; }

    private IList<PlannedElectricityCostAdjustmentFactor> _plannedElectricityCostAdjustmentFactors = new List<PlannedElectricityCostAdjustmentFactor>();
    public virtual IReadOnlyCollection<PlannedElectricityCostAdjustmentFactor> PlannedElectricityCostAdjustmentFactors => _plannedElectricityCostAdjustmentFactors.AsReadOnly();
    // Abstract methods
    public abstract double GetElectricityConsumePerMetres();
    public abstract double GetElectricityCostPerMetres();

    // Common method
    public double GetCurrentElectricityCost()
    {
        return Equipment?.GetEffectiveDateCost(StartMonth) ?? 0;
    }

    protected void ValidateDateRange(DateOnly startMonth, DateOnly endMonth)
    {
        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }
    }
}