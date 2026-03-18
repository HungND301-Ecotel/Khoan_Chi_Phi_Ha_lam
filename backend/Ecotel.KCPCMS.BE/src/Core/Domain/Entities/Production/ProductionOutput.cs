using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Production;

public class ProductionOutput : AuditableEntity<Guid>, IAggregateRoot
{
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public double ProductionMeters { get; protected set; }
    public double StandardProductionMeters { get; protected set; }

    //Navigation Properties
    public virtual AcceptanceReport? AcceptanceReport { get; protected set; }

    private IList<ProductionOutputProcessGroup> _productionOutputProcessGroups = new List<ProductionOutputProcessGroup>();
    public virtual IReadOnlyCollection<ProductionOutputProcessGroup> ProductionOutputProcessGroups => _productionOutputProcessGroups.AsReadOnly();

    private IList<ProductUnitPriceProductionOutput> _productUnitPriceProductionOutputs = new List<ProductUnitPriceProductionOutput>();
    public virtual IReadOnlyCollection<ProductUnitPriceProductionOutput> ProductUnitPriceProductionOutputs => _productUnitPriceProductionOutputs.AsReadOnly();

    public static ProductionOutput Create(DateOnly startMonth, DateOnly endMonth, double productionMeters, double standardProductionMeters)
    {
        return new ProductionOutput
        {
            StartMonth = startMonth,
            EndMonth = endMonth,
            ProductionMeters = productionMeters,
            StandardProductionMeters = standardProductionMeters
        };
    }

    public void Update(DateOnly startMonth, DateOnly endMonth, double productionMeters, double standardProductionMeters)
    {
        StartMonth = startMonth;
        EndMonth = endMonth;
        ProductionMeters = productionMeters;
        StandardProductionMeters = standardProductionMeters;
    }

    public void SetProcessGroups(IEnumerable<ProductionOutputProcessGroup> processGroups)
    {
        _productionOutputProcessGroups = processGroups.ToList();
        RecalculateFromProcessGroups();
    }

    public void ClearProcessGroups()
    {
        _productionOutputProcessGroups.Clear();
        ProductionMeters = 0;
        StandardProductionMeters = 0;
    }

    public void RecalculateFromProcessGroups()
    {
        StandardProductionMeters = _productionOutputProcessGroups.Sum(x => x.StandardProductionMeters);
        ProductionMeters = _productionOutputProcessGroups.Sum(x => x.ProductionMeters);
    }
}
