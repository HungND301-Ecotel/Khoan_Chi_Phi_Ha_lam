namespace Application.Dto.Catalog.TransportPlanLine;

public class AdjustmentFactorSelectionDto
{
    public Guid? AdjustmentFactorDescriptionId { get; set; }
    public Guid? AdjustmentFactorId { get; set; }
    public double? CustomValue { get; set; }
}


// 1 dòng trong bảng "Thành phần chi phí" ở màn Xem chi tiết kế hoạch (Vật liệu / SCTX / Động lực).
// K1Coefficient/K2Coefficient tách riêng (không gộp nhân) để FE hiển thị đúng giá trị từng hệ số.
public class TransportCostComponentDto
{
    public decimal? BaseUnitPrice { get; set; }
    public double? K1Coefficient { get; set; }
    public double? K2Coefficient { get; set; }
    public double EffectiveUnitPrice { get; set; }
}

public class TransportPlanLineItemDto
{
    public Guid Id { get; set; }
    public Guid? PlannedTransportCostId { get; set; }
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessCode { get; set; } = string.Empty;
    public string ProductionProcessName { get; set; } = string.Empty;
    public Guid? TransportRouteId { get; set; }
    public string? TransportRouteCode { get; set; }
    public string? TransportRouteName { get; set; }
    public Guid? RouteDepartmentId { get; set; }
    public string? RouteDepartmentCode { get; set; }
    public string? RouteDepartmentName { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentQuality { get; set; }
    public double ProductionMeters { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public AdjustmentFactorSelectionDto? K1 { get; set; }
    public AdjustmentFactorSelectionDto? K2 { get; set; }
    // Tuyến đặc biệt (sản lượng < 10.000 tấn/tháng): true khi Material/Maintenance/Power bên dưới
    // đã là giá trọn gói cho cả tuyến/tháng
    public bool IsLowVolumeCase { get; set; }
    public TransportCostComponentDto Material { get; set; } = new();
    public TransportCostComponentDto Maintenance { get; set; } = new();
    public TransportCostComponentDto Power { get; set; } = new();
    public double PlannedTotalCost { get; set; }
}

public class TransportPlanLineMonthDto
{
    public DateOnly Month { get; set; }

    public bool LowValuePerishableSupply { get; set; }
    public IList<TransportPlanLineItemDto> Items { get; set; } = new List<TransportPlanLineItemDto>();
}

public class TransportPlanLineByDepartmentDetailDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public IList<TransportPlanLineMonthDto> Months { get; set; } = new List<TransportPlanLineMonthDto>();
}


public class TransportPlanLineAdjustmentItemDto
{
    public Guid Id { get; set; }
    public Guid ProductionProcessId { get; set; }
    public string ProductionProcessCode { get; set; } = string.Empty;
    public string ProductionProcessName { get; set; } = string.Empty;
    public Guid? TransportRouteId { get; set; }
    public string? TransportRouteCode { get; set; }
    public string? TransportRouteName { get; set; }
    public Guid? RouteDepartmentId { get; set; }
    public string? RouteDepartmentCode { get; set; }
    public string? RouteDepartmentName { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentQuality { get; set; }
    public double ActualProductionMeters { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public string? UnitOfMeasureName { get; set; }
    public AdjustmentFactorSelectionDto? K1 { get; set; }
    public AdjustmentFactorSelectionDto? K2 { get; set; }
    public bool IsLowVolumeCase { get; set; }
    public TransportCostComponentDto Material { get; set; } = new();
    public TransportCostComponentDto Maintenance { get; set; } = new();
    public TransportCostComponentDto Power { get; set; } = new();
    public double AdjustmentTotalCost { get; set; }
}

public class TransportPlanLineAdjustmentMonthDto
{
    public DateOnly Month { get; set; }
    public IList<TransportPlanLineAdjustmentItemDto> Items { get; set; } = new List<TransportPlanLineAdjustmentItemDto>();
}

public class TransportPlanLineAdjustmentByDepartmentDetailDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public IList<TransportPlanLineAdjustmentMonthDto> Months { get; set; } = new List<TransportPlanLineAdjustmentMonthDto>();
}


public class CreateTransportPlanLineByDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public IList<CreateTransportPlanLineMonthDto> Months { get; set; } = new List<CreateTransportPlanLineMonthDto>();
}

public class CreateTransportPlanLineMonthDto
{
    public DateOnly Month { get; set; }
    public bool LowValuePerishableSupply { get; set; }
    public IList<CreateTransportPlanLineItemDto> Items { get; set; } = new List<CreateTransportPlanLineItemDto>();
}

public class CreateTransportPlanLineItemDto
{
    public Guid ProductionProcessId { get; set; }
    public Guid? TransportRouteId { get; set; }
    public Guid? RouteDepartmentId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentQuality { get; set; }
    public double ProductionMeters { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public AdjustmentFactorSelectionDto? K1 { get; set; }
    public AdjustmentFactorSelectionDto? K2 { get; set; }
}


public class UpdateTransportPlanLineByDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public IList<UpdateTransportPlanLineMonthDto> Months { get; set; } = new List<UpdateTransportPlanLineMonthDto>();
}

public class UpdateTransportPlanLineMonthDto
{
    public DateOnly Month { get; set; }
    public bool LowValuePerishableSupply { get; set; }
    public IList<UpdateTransportPlanLineItemDto> Items { get; set; } = new List<UpdateTransportPlanLineItemDto>();
}

public class UpdateTransportPlanLineItemDto
{
    public Guid? TransportPlanLineId { get; set; }
    public Guid ProductionProcessId { get; set; }
    public Guid? TransportRouteId { get; set; }
    public Guid? RouteDepartmentId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentQuality { get; set; }
    public double ProductionMeters { get; set; }
    public Guid? UnitOfMeasureId { get; set; }
    public AdjustmentFactorSelectionDto? K1 { get; set; }
    public AdjustmentFactorSelectionDto? K2 { get; set; }
}
