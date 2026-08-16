using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class ProductionOutputProcessGroup : AuditableEntity<Guid>
{
    public Guid ProductionOutputId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public double PlanProductionMeters { get; protected set; }
    public double StandardProductionMeters { get; protected set; }
    public double ProductionMeters { get; protected set; }

    // Navigation properties
    public virtual ProductionOutput? ProductionOutput { get; protected set; }
    public virtual ProcessGroup? ProcessGroup { get; protected set; }

    private IList<ProductionOutputProduct> _productionOutputProducts = new List<ProductionOutputProduct>();
    public virtual IReadOnlyCollection<ProductionOutputProduct> ProductionOutputProducts => _productionOutputProducts.AsReadOnly();

    // Vận tải lò/Vận tải cơ giới — song song ProductionOutputProducts (Khai thác), loại trừ lẫn nhau
    // theo ProcessGroup của nhóm này.
    private IList<ProductionOutputTransportLine> _productionOutputTransportLines = new List<ProductionOutputTransportLine>();
    public virtual IReadOnlyCollection<ProductionOutputTransportLine> ProductionOutputTransportLines => _productionOutputTransportLines.AsReadOnly();

    public static ProductionOutputProcessGroup Create(
        Guid processGroupId,
        double planProductionMeters,
        double standardProductionMeters)
    {
        return new ProductionOutputProcessGroup
        {
            ProcessGroupId = processGroupId,
            PlanProductionMeters = planProductionMeters,
            StandardProductionMeters = standardProductionMeters
        };
    }

    public void Update(double planProductionMeters, double standardProductionMeters)
    {
        PlanProductionMeters = planProductionMeters;
        StandardProductionMeters = standardProductionMeters;
    }

    public void AddProduct(ProductionOutputProduct product)
    {
        _productionOutputProducts.Add(product);
        RecalculateProductionMeters();
    }

    public void ClearProducts()
    {
        _productionOutputProducts.Clear();
        RecalculateProductionMeters();
    }

    public void AddTransportLine(ProductionOutputTransportLine transportLine)
    {
        _productionOutputTransportLines.Add(transportLine);
        RecalculateProductionMeters();
    }

    public void ClearTransportLines()
    {
        _productionOutputTransportLines.Clear();
        RecalculateProductionMeters();
    }

    public void RecalculateProductionMeters()
    {
        ProductionMeters = _productionOutputProducts.Sum(x => x.ProductionMeters)
            + _productionOutputTransportLines.Sum(x => x.ProductionMeters);
    }
}
