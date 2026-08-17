using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Shared.Constants;

namespace Domain.Entities.Index;

public class PlannedTransportCostAdjustmentFactor : AuditableEntity<Guid>
{
    public Guid PlannedTransportCostId { get; protected set; }
    public Guid? AdjustmentFactorDescriptionId { get; protected set; }
    public Guid? AdjustmentFactorId { get; protected set; }
    public double? CustomValue { get; protected set; }

    public double MaintenanceEffectiveValue => CustomValue ?? AdjustmentFactorDescription?.MaintenanceAdjustmentValue ?? 1;
    public double PowerEffectiveValue => CustomValue ?? AdjustmentFactorDescription?.ElectricityAdjustmentValue ?? 1;

    // Navigation properties
    public virtual PlannedTransportCost? PlannedTransportCost { get; protected set; }
    public virtual AdjustmentFactorDescription? AdjustmentFactorDescription { get; protected set; }
    public virtual AdjustmentFactor? AdjustmentFactor { get; protected set; }

    public static PlannedTransportCostAdjustmentFactor Create(
        Guid plannedTransportCostId,
        Guid? adjustmentFactorDescriptionId,
        Guid? adjustmentFactorId,
        double? customValue)
    {
        Validate(plannedTransportCostId, adjustmentFactorDescriptionId, adjustmentFactorId, customValue);

        return new PlannedTransportCostAdjustmentFactor
        {
            PlannedTransportCostId = plannedTransportCostId,
            AdjustmentFactorDescriptionId = adjustmentFactorDescriptionId,
            AdjustmentFactorId = adjustmentFactorId,
            CustomValue = customValue,
        };
    }

    private static void Validate(
        Guid plannedTransportCostId,
        Guid? adjustmentFactorDescriptionId,
        Guid? adjustmentFactorId,
        double? customValue)
    {
        if (plannedTransportCostId == Guid.Empty)
        {
            throw new ArgumentException(CustomResponseMessage.PlannedTransportCostNotFound);
        }

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
    }
}
