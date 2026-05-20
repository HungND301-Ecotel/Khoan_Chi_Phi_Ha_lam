namespace Domain.Entities.Production;

public class ProductionReference
{
    public Guid? ProductionOrderId { get; private set; }
    public Guid? EquipmentId { get; private set; }
    public Guid? AssignmentCodeId => EquipmentId;

    private ProductionReference(Guid? productionOrderId, Guid? assignmentCodeId)
    {
        // Keep both values for downstream lookup (e.g. Part + Equipment + period).
        // Grouping/classification priority (ProductionOrder first) is handled by consumers.
        ProductionOrderId = productionOrderId;
        EquipmentId = assignmentCodeId;
    }

    public static ProductionReference Create(Guid? productionOrderId, Guid? equipmentId)
        => new(productionOrderId, equipmentId);

    public static ProductionReference CreateForAssignmentCode(Guid? productionOrderId, Guid? assignmentCodeId)
        => new(productionOrderId, assignmentCodeId);

    public static ProductionReference Empty()
        => new(null, null);
}

