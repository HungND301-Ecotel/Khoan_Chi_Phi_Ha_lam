namespace Domain.Entities.Production;

public class ProductionReference
{
    public Guid? ProductionOrderId { get; private set; }
    public Guid? EquipmentId { get; private set; }

    private ProductionReference(Guid? productionOrderId, Guid? equipmentId)
    {
        // Keep both values for downstream lookup (e.g. Part + Equipment + period).
        // Grouping/classification priority (ProductionOrder first) is handled by consumers.
        ProductionOrderId = productionOrderId;
        EquipmentId = equipmentId;
    }

    public static ProductionReference Create(Guid? productionOrderId, Guid? equipmentId)
        => new(productionOrderId, equipmentId);

    public static ProductionReference Empty()
        => new(null, null);
}

