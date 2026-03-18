using Domain.Common.Contracts;

namespace Domain.Entities.Pricing;

public class PlannedElectricityCost : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid OutputId { get; protected set; }

    private double? CachedPlannedElectricityTotal { get; set; }

    //NavigationProperty
    public ProductUnitPrice? ProductUnitPrice { get; protected set; }
    public Output Output { get; protected set; }

    private IList<PlannedElectricityCostAdjustmentFactor> _plannedElectricityCostAdjustmentFactors = new List<PlannedElectricityCostAdjustmentFactor>();
    public virtual IReadOnlyCollection<PlannedElectricityCostAdjustmentFactor> PlannedElectricityCostAdjustmentFactors => _plannedElectricityCostAdjustmentFactors.AsReadOnly();

    //Constructor
    public double GetPlannedTotalPrice()
    {
        if (CachedPlannedElectricityTotal.HasValue)
        {
            return CachedPlannedElectricityTotal.Value;
        }

        CachedPlannedElectricityTotal = _plannedElectricityCostAdjustmentFactors.Sum(p => p.GetCurrentElectricityCost());
        return CachedPlannedElectricityTotal.Value;
    }

    public static PlannedElectricityCost Create(Guid productUnitPriceId, Guid outputId, IEnumerable<PlannedElectricityCostAdjustmentFactor> list)
    {
        var result = new PlannedElectricityCost
        {
            ProductUnitPriceId = productUnitPriceId,
            OutputId = outputId
        };
        result.AddPlannedElectricityCostAdjustmentFactors(list.ToList());
        return result;
    }

    public void Update(Guid productUnitPriceId, Guid outputId)
    {
        ProductUnitPriceId = productUnitPriceId;
        OutputId = outputId;
    }

    public void ClearPlannedElectricityCostAdjustmentFactors()
    {
        _plannedElectricityCostAdjustmentFactors.Clear();
    }

    public void AddPlannedElectricityCostAdjustmentFactors(IList<PlannedElectricityCostAdjustmentFactor> list)
    {
        foreach (var item in list)
        {
            _plannedElectricityCostAdjustmentFactors.Add(item);
        }
    }
}
