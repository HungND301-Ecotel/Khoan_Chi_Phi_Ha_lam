namespace Domain.Entities.Production;

public class ProductionReference
{
    public Guid? ProductionOrderId { get; private set; }
    public Guid? EquipmentId { get; private set; }

    private ProductionReference(Guid? productionOrderId, Guid? equipmentId)
    {
        if (productionOrderId.HasValue && equipmentId.HasValue)
        {
            throw new ArgumentException("Chỉ được chọn một trong hai: Lệnh sản xuất hoặc Thiết bị");
        }

        ProductionOrderId = productionOrderId;
        EquipmentId = equipmentId;
    }

    public static ProductionReference Create(Guid? productionOrderId, Guid? equipmentId)
        => new(productionOrderId, equipmentId);

    public static ProductionReference Empty()
        => new(null, null);
}

