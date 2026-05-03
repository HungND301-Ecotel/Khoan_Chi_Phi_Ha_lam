using Domain.Common.Contracts;

namespace Domain.Entities.Pricing;

public class PlannedMaintainCost : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid OutputId { get; protected set; }
    public double TrimmingCoefficient { get; protected set; } = 1;

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
            return CachedPlannedMaintainTotal.Value * GetNormalizedTrimmingCoefficient();
        }

        CachedPlannedMaintainTotal = _plannedMaintainCostAdjustmentFactors.Sum(p => p.GetCurrentMaintainCost());
        return CachedPlannedMaintainTotal.Value * GetNormalizedTrimmingCoefficient();
    }

    public static PlannedMaintainCost Create(
        Guid productUnitPriceId,
        Guid outputId,
        double trimmingCoefficient,
        IEnumerable<PlannedMaintainCostAdjustmentFactor> list)
    {
        var result = new PlannedMaintainCost
        {
            ProductUnitPriceId = productUnitPriceId,
            OutputId = outputId
        };
        result.SetTrimmingCoefficient(trimmingCoefficient);
        result.AddPlannedMaintainCostAdjustmentFactors(list.ToList());
        return result;
    }

    public void Update(Guid productUnitPriceId, Guid outputId, double trimmingCoefficient)
    {
        ProductUnitPriceId = productUnitPriceId;
        OutputId = outputId;
        SetTrimmingCoefficient(trimmingCoefficient);
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

    private static double NormalizeTrimmingCoefficient(double trimmingCoefficient)
    {
        if (trimmingCoefficient <= 0)
        {
            return 1;
        }

        return trimmingCoefficient > 1 ? trimmingCoefficient / 100 : trimmingCoefficient;
    }

    private double GetNormalizedTrimmingCoefficient()
    {
        return NormalizeTrimmingCoefficient(TrimmingCoefficient);
    }

    private void SetTrimmingCoefficient(double trimmingCoefficient)
    {
        TrimmingCoefficient = NormalizeTrimmingCoefficient(trimmingCoefficient);
    }
}
