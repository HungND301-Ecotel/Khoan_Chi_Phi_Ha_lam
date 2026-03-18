using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class ProductionOutputProcessGroup : AuditableEntity<Guid>
{
    public Guid ProductionOutputId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public double StandardProductionMeters { get; protected set; }
    public double ProductionMeters { get; protected set; }

    // Navigation properties
    public virtual ProductionOutput? ProductionOutput { get; protected set; }
    public virtual ProcessGroup? ProcessGroup { get; protected set; }

    private IList<ProductionOutputProduct> _productionOutputProducts = new List<ProductionOutputProduct>();
    public virtual IReadOnlyCollection<ProductionOutputProduct> ProductionOutputProducts => _productionOutputProducts.AsReadOnly();

    public static ProductionOutputProcessGroup Create(Guid processGroupId, double standardProductionMeters)
    {
        return new ProductionOutputProcessGroup
        {
            ProcessGroupId = processGroupId,
            StandardProductionMeters = standardProductionMeters
        };
    }

    public void Update(double standardProductionMeters)
    {
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

    public void RecalculateProductionMeters()
    {
        ProductionMeters = _productionOutputProducts.Sum(x => x.ProductionMeters);
    }
}
