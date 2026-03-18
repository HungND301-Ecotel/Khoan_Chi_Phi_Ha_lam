using Domain.Common.Contracts;

namespace Domain.Entities.Pricing;

public class PlannedMaintainCost : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid OutputId { get; protected set; }

    private double? CachedPlannedMaintainTotal { get; set; }

    //NavigationProperty
    public ProductUnitPrice? ProductUnitPrice { get; protected set; }
    public Output Output { get; protected set; }

    private IList<PlannedMaintainCostAdjustmentFactor> _plannedMaintainCostAdjustmentFactors = new List<PlannedMaintainCostAdjustmentFactor>();
    public virtual IReadOnlyCollection<PlannedMaintainCostAdjustmentFactor> PlannedMaintainCostAdjustmentFactors => _plannedMaintainCostAdjustmentFactors.AsReadOnly();

    //Constructor
    public double GetPlannedTotalPrice()
    {
        if (CachedPlannedMaintainTotal.HasValue)
        {
            return CachedPlannedMaintainTotal.Value;
        }

        CachedPlannedMaintainTotal = _plannedMaintainCostAdjustmentFactors.Sum(p => p.GetCurrentMaintainCost());
        return CachedPlannedMaintainTotal.Value;
    }

    public static PlannedMaintainCost Create(Guid productUnitPriceId, Guid outputId, IEnumerable<PlannedMaintainCostAdjustmentFactor> list)
    {
        var result = new PlannedMaintainCost
        {
            ProductUnitPriceId = productUnitPriceId,
            OutputId = outputId
        };
        result.AddPlannedMaintainCostAdjustmentFactors(list.ToList());
        return result;
    }

    public void Update(Guid productUnitPriceId, Guid outputId)
    {
        ProductUnitPriceId = productUnitPriceId;
        OutputId = outputId;
    }

    public void ClearPlannedMaintainCostAdjustmentFactors()
    {
        _plannedMaintainCostAdjustmentFactors.Clear();
    }

    public void AddPlannedMaintainCostAdjustmentFactors(IList<PlannedMaintainCostAdjustmentFactor> list)
    {
        foreach (var item in list)
        {
            _plannedMaintainCostAdjustmentFactors.Add(item);
        }
    }
}
