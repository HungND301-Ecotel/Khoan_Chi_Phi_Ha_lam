using Domain.Common.Contracts;
using Shared.Constants;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index;
public class PlannedElectricityCostAdjustmentFactorDescription : AuditableEntity<Guid>
{
    public Guid PlannedElectricityCostAdjustmentFactorId { get; set; }
    public Guid? AdjustmentFactorDescriptionId { get; set; }
    public Guid? AdjustmentFactorId { get; set; }
    public double? CustomValue { get; set; }

    public double EffectiveValue => CustomValue ?? AdjustmentFactorDescription?.ElectricityAdjustmentValue ?? 1;

    public static PlannedElectricityCostAdjustmentFactorDescription Create(
        Guid plannedElectricityCostAdjustmentFactorId,
        Guid? adjustmentFactorDescriptionId,
        Guid? adjustmentFactorId,
        double? customValue)
    {
        var hasAdjustmentFactorDescription = adjustmentFactorDescriptionId.HasValue;
        var hasCustomValue = adjustmentFactorId.HasValue || customValue.HasValue;

        if (hasAdjustmentFactorDescription == hasCustomValue)
        {
            throw new ArgumentException(CustomResponseMessage.InvalidParams);
        }

        if (hasCustomValue && (!adjustmentFactorId.HasValue || !customValue.HasValue))
        {
            throw new ArgumentException(CustomResponseMessage.InvalidParams);
        }

        return new PlannedElectricityCostAdjustmentFactorDescription
        {
            PlannedElectricityCostAdjustmentFactorId = plannedElectricityCostAdjustmentFactorId,
            AdjustmentFactorDescriptionId = adjustmentFactorDescriptionId,
            AdjustmentFactorId = adjustmentFactorId,
            CustomValue = customValue
        };
    }

    //Navigation Properties
    public virtual PlannedElectricityCostAdjustmentFactor? PlannedElectricityCostAdjustmentFactor { get; set; }
    public virtual AdjustmentFactorDescription? AdjustmentFactorDescription { get; set; }
    public virtual AdjustmentFactor? AdjustmentFactor { get; set; }
}
