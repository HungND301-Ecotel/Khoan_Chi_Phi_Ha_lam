using Domain.Common.Contracts;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Pricing;

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
                   ElectricityUnitPriceEquipment.GetElectricityCostPerMetres() *
                   _plannedElectricityCostAdjustmentFactorDescriptions.Aggregate(1.0, (acc, x) => acc * x.AdjustmentFactorDescription.MaintenanceAdjustmentValue ?? 1);
            return CachedAdjustmentFactorTotal.Value;
        }

        return 0;
    }

    public static PlannedElectricityCostAdjustmentFactor Create(
        Guid plannedElectricityCostId,
        Guid electricityUnitPriceId,
        decimal quantity,
        List<AdjustmentFactorDescription?> adjustmentFactorDescriptions)
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

    public void AddAdjustmentFactorDescription(IList<AdjustmentFactorDescription?> adjustmentFactorDescriptions)
    {
        foreach (var adj in adjustmentFactorDescriptions)
        {
            if (adj == null)
            {
                throw new ArgumentException(CustomResponseMessage.AdjustmentFactorDescriptionIsNull);
            }
            _plannedElectricityCostAdjustmentFactorDescriptions.Add(new PlannedElectricityCostAdjustmentFactorDescription
            {
                AdjustmentFactorDescriptionId = adj.Id,
                PlannedElectricityCostAdjustmentFactorId = this.Id
            });
        }
    }
}
