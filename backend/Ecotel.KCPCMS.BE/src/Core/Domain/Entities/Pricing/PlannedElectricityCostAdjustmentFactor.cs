using Domain.Common.Contracts;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Pricing;

public sealed record PlannedElectricityAdjustmentFactorInput(
    Guid? AdjustmentFactorDescriptionId,
    Guid? AdjustmentFactorId,
    double? CustomValue,
    AdjustmentFactorDescription? AdjustmentFactorDescription);

public class PlannedElectricityCostAdjustmentFactor : AuditableEntity<Guid>
{
    public Guid PlannedElectricityCostId { get; protected set; }
    public Guid ElectricityUnitPriceId { get; protected set; }
    public decimal Quantity { get; protected set; }

    private double? CachedAdjustmentFactorTotal { get; set; }

    //Navigation Properties
    public PlannedElectricityCost? PlannedElectricityCost { get; protected set; }
    public ElectricityUnitPriceEquipment? ElectricityUnitPriceEquipment { get; protected set; }


    private IList<PlannedElectricityCostAdjustmentFactorDescription> _plannedElectricityCostAdjustmentFactorDescriptions = new List<PlannedElectricityCostAdjustmentFactorDescription>();
    public virtual IReadOnlyCollection<PlannedElectricityCostAdjustmentFactorDescription> PlannedElectricityCostAdjustmentFactorDescriptions => _plannedElectricityCostAdjustmentFactorDescriptions.AsReadOnly();

    //Constructor
    public double GetCurrentElectricityCost()
    {
        if (ElectricityUnitPriceEquipment != null && _plannedElectricityCostAdjustmentFactorDescriptions.Any())
        {
            if (CachedAdjustmentFactorTotal.HasValue)
            {
                return CachedAdjustmentFactorTotal.Value;
            }

            CachedAdjustmentFactorTotal = (double)Quantity *
                     ElectricityUnitPriceEquipment.GetRoundedElectricityCostPerMetres() *
             _plannedElectricityCostAdjustmentFactorDescriptions.Aggregate(1.0, (acc, x) => acc * x.EffectiveValue);
            return CachedAdjustmentFactorTotal.Value;
        }

        return 0;
    }

    public static PlannedElectricityCostAdjustmentFactor Create(
        Guid plannedElectricityCostId,
        Guid electricityUnitPriceId,
        decimal quantity,
        IList<PlannedElectricityAdjustmentFactorInput> adjustmentFactorDescriptions)
    {
        var result = new PlannedElectricityCostAdjustmentFactor
        {
            PlannedElectricityCostId = plannedElectricityCostId,
            ElectricityUnitPriceId = electricityUnitPriceId,
            Quantity = quantity
        };
        result.AddAdjustmentFactorDescription(adjustmentFactorDescriptions);
        return result;
    }

    public void AddAdjustmentFactorDescription(IList<PlannedElectricityAdjustmentFactorInput> adjustmentFactorDescriptions)
    {
        foreach (var adj in adjustmentFactorDescriptions)
        {
            if (adj.AdjustmentFactorDescriptionId.HasValue && adj.AdjustmentFactorDescription == null)
            {
                throw new ArgumentException(CustomResponseMessage.AdjustmentFactorDescriptionIsNull);
            }

            _plannedElectricityCostAdjustmentFactorDescriptions.Add(
                PlannedElectricityCostAdjustmentFactorDescription.Create(
                    this.Id,
                    adj.AdjustmentFactorDescriptionId,
                    adj.AdjustmentFactorId,
                    adj.CustomValue));
        }
    }
}
