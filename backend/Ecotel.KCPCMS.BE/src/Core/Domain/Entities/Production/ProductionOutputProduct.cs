using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class ProductionOutputProduct : AuditableEntity<Guid>
{
    public Guid ProductionOutputProcessGroupId { get; protected set; }
    public Guid ProductId { get; protected set; }
    public double ProductionMeters { get; protected set; }

    // Navigation properties
    public virtual ProductionOutputProcessGroup? ProductionOutputProcessGroup { get; protected set; }
    public virtual Product? Product { get; protected set; }

    public static ProductionOutputProduct Create(Guid productId, double productionMeters)
    {
        return new ProductionOutputProduct
        {
            ProductId = productId,
            ProductionMeters = productionMeters
        };
    }

    public void Update(double productionMeters)
    {
        ProductionMeters = productionMeters;
    }
}
