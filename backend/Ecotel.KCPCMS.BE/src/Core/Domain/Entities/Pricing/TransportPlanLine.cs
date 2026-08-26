using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing;

/// <summary>
/// Một dòng sản lượng kế hoạch/thực hiện của Vận tải lò (VTL) hoặc Vận tải cơ giới (VTCG).
/// Gộp chung 1 bảng cho cả 2 nhóm công đoạn — loại nào (VTL/VTCG) 
/// được suy ra qua ProductionProcess.ProcessGroupId, không có cột phân biệt riêng.
/// </summary>
public class TransportPlanLine : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid DepartmentId { get; protected set; }
    public Guid ProductionProcessId { get; protected set; }
    public double ProductionMeters { get; protected set; }
    public Guid? UnitOfMeasureId { get; protected set; }
    public ProductUnitPriceScenarioType ScenarioType { get; protected set; }
    public OutputType OutputType { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }

    // Dùng chung: Monoray/Thiết bị khác (VTL) và mọi loại xe (VTCG) đều trỏ AssignmentCode
    public Guid? EquipmentId { get; protected set; }
    public string? EquipmentQuality { get; protected set; }

    // Chỉ dùng khi ProductionProcess thuộc VTL, công đoạn 1-3 (than/đá qua băng tải, vận tải trục)
    public Guid? TransportRouteId { get; protected set; }
    // "Đơn vị áp dụng cho tuyến này" — chỉ VTL công đoạn 1 (than/đá qua băng tải)
    public Guid? RouteDepartmentId { get; protected set; }

    // Chỉ dùng khi ProductionProcess thuộc VTCG
    public Guid? HaulDistanceId { get; protected set; }
    public Guid? CargoTypeId { get; protected set; }
    public Guid? ReceivingLocationId { get; protected set; }
    public Guid? DumpingLocationId { get; protected set; }

    // Navigation properties
    public virtual Department? Department { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }
    public virtual AssignmentCode? Equipment { get; protected set; }
    public virtual TransportRoute? TransportRoute { get; protected set; }
    public virtual Department? RouteDepartment { get; protected set; }
    public virtual HaulDistance? HaulDistance { get; protected set; }
    public virtual CargoType? CargoType { get; protected set; }
    public virtual TransportLocation? ReceivingLocation { get; protected set; }
    public virtual TransportLocation? DumpingLocation { get; protected set; }
    public virtual PlannedTransportCost? PlannedTransportCost { get; protected set; }

    public static TransportPlanLine Create(
        Guid departmentId,
        Guid productionProcessId,
        double productionMeters,
        Guid? unitOfMeasureId,
        ProductUnitPriceScenarioType scenarioType,
        OutputType outputType,
        DateOnly startMonth,
        DateOnly endMonth,
        Guid? equipmentId,
        string? equipmentQuality,
        Guid? transportRouteId,
        Guid? routeDepartmentId,
        Guid? haulDistanceId,
        Guid? cargoTypeId = null,
        Guid? receivingLocationId = null,
        Guid? dumpingLocationId = null)
    {
        Validate(departmentId, productionProcessId, productionMeters, startMonth, endMonth);

        return new TransportPlanLine
        {
            DepartmentId = departmentId,
            ProductionProcessId = productionProcessId,
            ProductionMeters = productionMeters,
            UnitOfMeasureId = unitOfMeasureId,
            ScenarioType = scenarioType,
            OutputType = outputType,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            EquipmentId = equipmentId,
            EquipmentQuality = equipmentQuality,
            TransportRouteId = transportRouteId,
            RouteDepartmentId = routeDepartmentId,
            HaulDistanceId = haulDistanceId,
            CargoTypeId = cargoTypeId,
            ReceivingLocationId = receivingLocationId,
            DumpingLocationId = dumpingLocationId,
        };
    }

    public void Update(
        Guid departmentId,
        Guid productionProcessId,
        double productionMeters,
        Guid? unitOfMeasureId,
        DateOnly startMonth,
        DateOnly endMonth,
        Guid? equipmentId,
        string? equipmentQuality,
        Guid? transportRouteId,
        Guid? routeDepartmentId,
        Guid? haulDistanceId,
        Guid? cargoTypeId = null,
        Guid? receivingLocationId = null,
        Guid? dumpingLocationId = null)
    {
        Validate(departmentId, productionProcessId, productionMeters, startMonth, endMonth);

        DepartmentId = departmentId;
        ProductionProcessId = productionProcessId;
        ProductionMeters = productionMeters;
        UnitOfMeasureId = unitOfMeasureId;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        EquipmentId = equipmentId;
        EquipmentQuality = equipmentQuality;
        TransportRouteId = transportRouteId;
        RouteDepartmentId = routeDepartmentId;
        HaulDistanceId = haulDistanceId;
        CargoTypeId = cargoTypeId;
        ReceivingLocationId = receivingLocationId;
        DumpingLocationId = dumpingLocationId;
    }

    private static void Validate(
        Guid departmentId,
        Guid productionProcessId,
        double productionMeters,
        DateOnly startMonth,
        DateOnly endMonth)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException(CustomResponseMessage.DepartmentIdCannotBeEmpty);
        }

        if (productionProcessId == Guid.Empty)
        {
            throw new ArgumentException(CustomResponseMessage.ProductionProcessIdCannotBeEmpty);
        }

        if (productionMeters <= 0)
        {
            throw new ArgumentException(CustomResponseMessage.ProductionMetersMustBeGreaterThanZero);
        }

        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }
    }
}
