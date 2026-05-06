using Domain.Common.Contracts;

namespace Domain.Entities.Pricing;

public class PlannedElectricityCost : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid OutputId { get; protected set; }
    public double TrimmingCoefficient { get; protected set; } = 1;

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
            return CachedPlannedElectricityTotal.Value * GetNormalizedTrimmingCoefficient();
        }

        CachedPlannedElectricityTotal = _plannedElectricityCostAdjustmentFactors.Sum(p => p.GetCurrentElectricityCost());
        return CachedPlannedElectricityTotal.Value * GetNormalizedTrimmingCoefficient();
    }

    public static PlannedElectricityCost Create(
        Guid productUnitPriceId,
        Guid outputId,
        double trimmingCoefficient,
        IEnumerable<PlannedElectricityCostAdjustmentFactor> list)
    {
        var result = new PlannedElectricityCost
        {
            ProductUnitPriceId = productUnitPriceId,
            OutputId = outputId
        };
        result.SetTrimmingCoefficient(trimmingCoefficient);
        result.AddPlannedElectricityCostAdjustmentFactors(list.ToList());
        return result;
    }

    public void Update(Guid productUnitPriceId, Guid outputId, double trimmingCoefficient)
    {
        ProductUnitPriceId = productUnitPriceId;
        OutputId = outputId;
        SetTrimmingCoefficient(trimmingCoefficient);
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
