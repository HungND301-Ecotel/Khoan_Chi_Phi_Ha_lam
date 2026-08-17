using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

/// <summary>
/// Sản lượng thực hiện của 1 dòng Vận tải lò/Vận tải cơ giới trong 1 <see cref="ProductionOutputProcessGroup"/>
/// — song song vai trò với <see cref="ProductionOutputProduct"/> bên Khai thác, nhưng thay
/// ProductId bằng (ProductionProcessId, EquipmentId, TransportRouteId) đúng field Kế hoạch VTL
/// đang dùng (xem claude/plan-dong-bo-van-tai.md mục 3.2).
/// </summary>
public class ProductionOutputTransportLine : AuditableEntity<Guid>
{
    public Guid ProductionOutputProcessGroupId { get; protected set; }
    public Guid ProductionProcessId { get; protected set; }
    public Guid? EquipmentId { get; protected set; }
    public string? EquipmentQuality { get; protected set; }
    public Guid? TransportRouteId { get; protected set; }
    // "Đơn vị áp dụng cho tuyến này" — chỉ công đoạn Vận tải qua băng tải (VTDQBT/VTTQBT) dùng,
    // đúng field RouteDepartmentId bên Kế hoạch (TransportPlanLine).
    public Guid? RouteDepartmentId { get; protected set; }
    public double ProductionMeters { get; protected set; }

    // Navigation properties
    public virtual ProductionOutputProcessGroup? ProductionOutputProcessGroup { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual AssignmentCode? Equipment { get; protected set; }
    public virtual TransportRoute? TransportRoute { get; protected set; }
    public virtual Department? RouteDepartment { get; protected set; }

    public static ProductionOutputTransportLine Create(
        Guid productionProcessId,
        Guid? equipmentId,
        string? equipmentQuality,
        Guid? transportRouteId,
        Guid? routeDepartmentId,
        double productionMeters)
    {
        return new ProductionOutputTransportLine
        {
            ProductionProcessId = productionProcessId,
            EquipmentId = equipmentId,
            EquipmentQuality = equipmentQuality,
            TransportRouteId = transportRouteId,
            RouteDepartmentId = routeDepartmentId,
            ProductionMeters = productionMeters,
        };
    }

    public void Update(double productionMeters)
    {
        ProductionMeters = productionMeters;
    }
}
