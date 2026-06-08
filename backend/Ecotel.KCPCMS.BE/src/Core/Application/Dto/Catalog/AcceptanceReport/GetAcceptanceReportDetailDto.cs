using Domain.Common.Enums;

namespace Application.Dto.Catalog.AcceptanceReport;

public record AcceptanceReportCategoryAllocationDetailDto
{
    public required Guid ProcessGroupId { get; init; }
    public string? ProcessGroupCode { get; init; }
    public string? ProcessGroupName { get; init; }
    public required double Quantity { get; init; }
    public List<Guid> AssignmentCodeIds { get; init; } = new();
    public List<Guid> EquipmentIds => AssignmentCodeIds;
}

public record AcceptanceReportDetailItemDto
{
    public const string NoneCategoryAssignmentCodeLabel = "[Nhóm vật tư, tài sản] Không thuộc nhóm vật tư, tài sản";
    public const string NoneCategoryProductionOrderLabel = "[Lệnh sản xuất] Không theo lệnh sản xuất";

    private Guid? _trackedMaterialId;
    private string? _trackedMaterialCode;
    private string? _trackedMaterialName;

    public required Guid Id { get; init; }
    public required Guid AcceptanceReportId { get; init; }
    public required Guid? CategoryProductionOrderId { get; init; }
    public string? CategoryProductionOrderLabel { get; init; }
    public Guid? CategoryAssignmentCodeId { get; init; }
    public string? CategoryAssignmentCodeLabel { get; init; }
    public Guid? CategoryEquipmentId => CategoryAssignmentCodeId;
    public Guid? AdditionalCostProductionOrderId { get; init; }
    public Guid? AdditionalCostAssignmentCodeId { get; init; }
    public Guid? AdditionalCostEquipmentId => AdditionalCostAssignmentCodeId;
    public Guid? MaterialId
    {
        get => _trackedMaterialId;
        init => _trackedMaterialId = value;
    }
    public Guid? PartId
    {
        get => _trackedMaterialId;
        init => _trackedMaterialId = value;
    }
    public Guid? TrackedMaterialId
    {
        get => _trackedMaterialId;
        init => _trackedMaterialId = value;
    }
    public double UsageTime { get; init; }
    public string? MaterialCode
    {
        get => _trackedMaterialCode;
        init => _trackedMaterialCode = value;
    }
    public string? MaterialName
    {
        get => _trackedMaterialName;
        init => _trackedMaterialName = value;
    }
    public string? PartCode
    {
        get => _trackedMaterialCode;
        init => _trackedMaterialCode = value;
    }
    public string? PartName
    {
        get => _trackedMaterialName;
        init => _trackedMaterialName = value;
    }
    public string? TrackedMaterialCode
    {
        get => _trackedMaterialCode;
        init => _trackedMaterialCode = value;
    }
    public string? TrackedMaterialName
    {
        get => _trackedMaterialName;
        init => _trackedMaterialName = value;
    }
    public PartType? PartType { get; init; }
    public string? UnitOfMeasureName { get; init; }

    public decimal PlanCost { get; init; }
    public decimal ActualCost { get; init; }

    public double IssuedQuantity { get; init; }
    public double ShippedQuantity { get; init; }

    public required List<IssuedDetailDto> IssuedDetails { get; init; }
    public required List<ShippedDetailDto> ShippedDetails { get; init; }

    public required ItemType ItemType { get; init; }

    public required AcceptanceReportItemType Type { get; init; }

    // Vật tư tính vào doanh thu khoán
    public AcceptanceReportItemType? MaterialsIncludedInContractRevenueType { get; init; }
    public required MaterialsIncludedInContractRevenue MaterialsIncludedInContractRevenue { get; init; }
    public required bool IsLongTermTracking { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public string? ProcessGroupCode { get; init; }
    public string? ProcessGroupName { get; init; }
    public required double MaterialsIncludedInContractRevenueQuantity { get; init; }
    public List<AcceptanceReportCategoryAllocationDetailDto> CategoryAllocations { get; init; } = new();

    // Bổ sung chi phí
    public required AdditionalCost AdditionalCostClassification { get; init; }
    public required AdditionalCost AdditionalCost { get; init; }
    public required OtherMaterialDetail OtherMaterialDetail { get; init; }
    public required double AdditionalCostQuantity { get; init; }

    // Vật tư theo hạn mức
    public required QuotaBasedMaterial QuotaBasedMaterial { get; init; }
    public required QuotaBasedMaterialType QuotaBasedMaterialType { get; init; }
    public List<QuotaBasedMaterialQuantityDto>? QuotaBasedMaterialQuantities { get; init; }

    // Tài sản
    public required Asset Asset { get; init; }
    public required double AssetMaterialQuantity { get; init; }
}

public record GetAcceptanceReportDetailDto
{
    public required Guid Id { get; init; }
    public required Guid ProductionOutputId { get; init; }
    public required string FilePath { get; init; }
    public required List<AcceptanceReportDetailItemDto> Items { get; init; }
}

