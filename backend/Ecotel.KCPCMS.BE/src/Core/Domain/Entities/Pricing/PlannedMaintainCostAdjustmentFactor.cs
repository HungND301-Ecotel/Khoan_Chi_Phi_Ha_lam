using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing;

public sealed record PlannedMaintainAdjustmentFactorInput(
    Guid? AdjustmentFactorDescriptionId,
    Guid? AdjustmentFactorId,
    double? CustomValue,
    AdjustmentFactorDescription? AdjustmentFactorDescription);

public class PlannedMaintainCostAdjustmentFactor : AuditableEntity<Guid>
{
    public Guid PlannedMaintainCostId { get; protected set; }
    public Guid MaintainUnitPriceId { get; protected set; }
    public decimal Quantity { get; protected set; }
    public double K6AdjustmentFactorValue { get; protected set; }

    private double? CachedPlannedMaintainAdjTotal { get; set; }

    //Navigation Properties
    public virtual PlannedMaintainCost? PlannedMaintainCost { get; protected set; }
    public virtual MaintainUnitPrice? MaintainUnitPrice { get; protected set; }


    private IList<PlannedMaintainCostAdjustmentFactorDescription> _plannedMaintainCostAdjustmentFactorDescriptions = new List<PlannedMaintainCostAdjustmentFactorDescription>();
    public virtual IReadOnlyCollection<PlannedMaintainCostAdjustmentFactorDescription> PlannedMaintainCostAdjustmentFactorDescriptions => _plannedMaintainCostAdjustmentFactorDescriptions.AsReadOnly();

    //Constructor
    public double GetCurrentMaintainCost()
    {
        if (PlannedMaintainCost is { Output: not null } && MaintainUnitPrice != null && _plannedMaintainCostAdjustmentFactorDescriptions.Any())
        {
            if (CachedPlannedMaintainAdjTotal.HasValue)
            {
                return CachedPlannedMaintainAdjTotal.Value;
            }

            var result = (double)Quantity *
                    MaintainUnitPrice.GetMaintainTotalPrice() *
                    K6AdjustmentFactorValue *
                    _plannedMaintainCostAdjustmentFactorDescriptions.Aggregate(1.0, (acc, x) => acc * x.EffectiveValue);

            CachedPlannedMaintainAdjTotal = result;
            return CachedPlannedMaintainAdjTotal.Value;
        }

        return 0;
    }

    public static PlannedMaintainCostAdjustmentFactor Create(
        Guid plannedMaintainCostId,
        Guid maintainUnitPriceId,
        decimal quantity,
        double k6AdjustmentFactorValue,
        IList<PlannedMaintainAdjustmentFactorInput> adjustmentFactorDescriptions)
    {
        var result = new PlannedMaintainCostAdjustmentFactor
        {
            PlannedMaintainCostId = plannedMaintainCostId,
            MaintainUnitPriceId = maintainUnitPriceId,
            Quantity = quantity,
            K6AdjustmentFactorValue = k6AdjustmentFactorValue
        };
        result.AddAdjustmentFactorDescription(adjustmentFactorDescriptions);
        return result;
    }

    public void AddAdjustmentFactorDescription(IList<PlannedMaintainAdjustmentFactorInput> adjustmentFactorDescriptions)
    {
        foreach (var adj in adjustmentFactorDescriptions)
        {
            if (adj.AdjustmentFactorDescriptionId.HasValue && adj.AdjustmentFactorDescription == null)
            {
                throw new ArgumentException(CustomResponseMessage.AdjustmentFactorDescriptionIsNull);
            }

            _plannedMaintainCostAdjustmentFactorDescriptions.Add(
                PlannedMaintainCostAdjustmentFactorDescription.Create(
                    this.Id,
                    adj.AdjustmentFactorDescriptionId,
                    adj.AdjustmentFactorId,
                    adj.CustomValue));
        }
    }
}
