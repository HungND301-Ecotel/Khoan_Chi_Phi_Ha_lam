using Domain.Common.Contracts;
using Domain.Entities.Production;

namespace Domain.Entities.Pricing;

public class ProductUnitPriceProductionOutput : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProductUnitPriceId { get; protected set; }
    public Guid ProductionOutputId { get; protected set; }
    public double ProductionMeters { get; protected set; }

    //Navigation properties
    public ProductUnitPrice? ProductUnitPrice { get; protected set; }
    public ProductionOutput? ProductionOutput { get; protected set; }

    public static ProductUnitPriceProductionOutput Create(Guid productUnitPriceId, Guid productionOutputId, double productionMeters = 0)
    {
        return new ProductUnitPriceProductionOutput
        {
            ProductUnitPriceId = productUnitPriceId,
            ProductionOutputId = productionOutputId,
            ProductionMeters = productionMeters
        };
    }

    public void UpdateProductionMeters(double productionMeters)
    {
        ProductionMeters = productionMeters;
    }
}
