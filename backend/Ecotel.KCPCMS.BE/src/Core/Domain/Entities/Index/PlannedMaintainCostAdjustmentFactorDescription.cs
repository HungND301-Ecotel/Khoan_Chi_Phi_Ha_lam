using Domain.Common.Contracts;
using Shared.Constants;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index;
public class PlannedMaintainCostAdjustmentFactorDescription : AuditableEntity<Guid>
{
    public Guid PlannedMaintainCostAdjustmentFactorId { get; set; }
    public Guid? AdjustmentFactorDescriptionId { get; set; }
    public Guid? AdjustmentFactorId { get; set; }
    public double? CustomValue { get; set; }

    public double EffectiveValue => CustomValue ?? AdjustmentFactorDescription?.MaintenanceAdjustmentValue ?? 1;

    public static PlannedMaintainCostAdjustmentFactorDescription Create(
        Guid plannedMaintainCostAdjustmentFactorId,
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

        return new PlannedMaintainCostAdjustmentFactorDescription
        {
            PlannedMaintainCostAdjustmentFactorId = plannedMaintainCostAdjustmentFactorId,
            AdjustmentFactorDescriptionId = adjustmentFactorDescriptionId,
            AdjustmentFactorId = adjustmentFactorId,
            CustomValue = customValue
        };
    }

    //Navigation Properties
    public virtual PlannedMaintainCostAdjustmentFactor? PlannedMaintainCostAdjustmentFactor { get; set; }
    public virtual AdjustmentFactorDescription? AdjustmentFactorDescription { get; set; }
    public virtual AdjustmentFactor? AdjustmentFactor { get; set; }
}
