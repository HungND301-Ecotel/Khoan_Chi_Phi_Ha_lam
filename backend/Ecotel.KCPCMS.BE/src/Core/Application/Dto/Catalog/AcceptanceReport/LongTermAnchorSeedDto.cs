using System.ComponentModel.DataAnnotations;

namespace Application.Dto.Catalog.AcceptanceReport;

public record LongTermAnchorSeedItemDto
{
    public Guid Id { get; init; }
    public Guid MaterialId { get; init; }
    public Guid? PartId => MaterialId;
    public Guid TrackedMaterialId { get; init; }
    public Guid ProcessGroupId { get; init; }
    public Guid? CategoryAssignmentCodeId { get; init; }
    public Guid? CategoryEquipmentId => CategoryAssignmentCodeId;
    public string? CategoryAssignmentCode { get; init; }
    public string? CategoryAssignmentCodeName { get; init; }
    public Guid? CategoryProductionOrderId { get; init; }
    public string? CategoryProductionOrderCode { get; init; }
    public string? CategoryProductionOrderName { get; init; }
    public string MaterialCode { get; init; } = string.Empty;
    public string MaterialName { get; init; } = string.Empty;
    public string? PartCode => TrackedMaterialCode;
    public string? PartName => TrackedMaterialName;
    public string TrackedMaterialCode { get; init; } = string.Empty;
    public string TrackedMaterialName { get; init; } = string.Empty;
    public string UnitOfMeasureName { get; init; } = string.Empty;
    public string ProcessGroupCode { get; init; } = string.Empty;
    public string ProcessGroupName { get; init; } = string.Empty;
    public decimal PendingValueStartPeriod { get; init; }
    public double UsageTime { get; init; }
    public double AllocatedTime { get; init; }
    public double RemainingTime { get; init; }
    public double AllocationRatio { get; init; }
    public string Note { get; init; } = string.Empty;
}

public record LongTermAnchorSeedProcessGroupMetricDto
{
    public Guid Id { get; init; }
    public Guid ProcessGroupId { get; init; }
    public string ProcessGroupCode { get; init; } = string.Empty;
    public string ProcessGroupName { get; init; } = string.Empty;
    public double PlannedOutput { get; init; }
    public double StandardOutput { get; init; }
}

public record GetLongTermAnchorSeedDetailResponseDto
{
    public Guid DepartmentId { get; init; }
    public string DepartmentCode { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public DateOnly? EffectiveMonth { get; init; }
    public List<LongTermAnchorSeedItemDto> Items { get; init; } = new();
    public List<LongTermAnchorSeedProcessGroupMetricDto> ProcessGroupMetrics { get; init; } = new();
}

public record UpdateLongTermAnchorSeedItemDto
{
    public Guid Id { get; init; }
    public Guid DepartmentId { get; init; }
    public Guid? TrackedMaterialId { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? PartId { get; init; }
    public Guid ProcessGroupId { get; init; }
    public Guid? CategoryAssignmentCodeId { get; init; }
    public Guid? CategoryEquipmentId => CategoryAssignmentCodeId;
    public Guid? CategoryProductionOrderId { get; init; }
    public double IssuedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal PendingValueStartPeriod { get; init; }
    public double UsageTime { get; init; }
    public double AllocatedTime { get; init; }
    public double AllocationRatio { get; init; }
    public string Note { get; init; } = string.Empty;
}

public record UpdateLongTermAnchorSeedProcessGroupMetricDto
{
    public Guid Id { get; init; }
    public Guid ProcessGroupId { get; init; }
    public double PlannedOutput { get; init; }
    public double StandardOutput { get; init; }
}

public record UpdateLongTermAnchorSeedRequestDto
{
    public Guid DepartmentId { get; init; }
    public List<UpdateLongTermAnchorSeedItemDto> Items { get; init; } = new();
    public List<UpdateLongTermAnchorSeedProcessGroupMetricDto> ProcessGroupMetrics { get; init; } = new();
}

public record LongTermAnchorSeedExcelRowDto
{
    [Display(Name = "id")]
    public Guid? Id { get; init; }

    [Display(Name = "Mã vật tư")]
    public string MaterialCode { get; init; } = string.Empty;

    [Display(Name = "Mã nhóm công đoạn")]
    public string ProcessGroupCode { get; init; } = string.Empty;

    [Display(Name = "Nhóm vật tư, tài sản")]
    public string CategoryAssignmentCode { get; init; } = string.Empty;

    [Display(Name = "Lệnh sản xuất")]
    public string CategoryProductionOrderCode { get; init; } = string.Empty;

    [Display(Name = "Tổng giá trị cần hạch toán (đ)")]
    public decimal? PendingValueStartPeriod { get; init; }

    [Display(Name = "Thời gian sử dụng (Ti)")]
    public double? UsageTime { get; init; }

    [Display(Name = "Thời gian đã phân bổ")]
    public double? AllocatedTime { get; init; }

    [Display(Name = "Ghi chú")]
    public string Note { get; init; } = string.Empty;
}
