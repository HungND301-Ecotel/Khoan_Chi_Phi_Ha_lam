using Domain.Common.Enums;

namespace Application.Dto.Catalog.AcceptanceReport;

public record AcceptanceReportCategoryAllocationDto
{
    private List<Guid> _assignmentCodeIds = new();

    public required Guid ProcessGroupId { get; init; }
    public required double Quantity { get; init; }
    public List<Guid> AssignmentCodeIds
    {
        get => _assignmentCodeIds;
        init => _assignmentCodeIds = value ?? [];
    }
    public List<Guid> EquipmentIds
    {
        get => _assignmentCodeIds;
        init => _assignmentCodeIds = value ?? [];
    }
}

public record CreateAcceptanceReportItemDto
{
    private Guid? _trackedMaterialId;
    private Guid? _categoryAssignmentCodeId;
    private Guid? _additionalCostAssignmentCodeId;

    public Guid? AcceptanceReportItemId { get; init; }

    public Guid? TrackedMaterialId
    {
        get => _trackedMaterialId;
        init => _trackedMaterialId = value;
    }
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
    public string? DocumentNumber { get; init; }
    public DateOnly? PostingDate { get; init; }
    public required double UsageTime { get; init; }

    public required AcceptanceReportItemType Type { get; init; }
    public required ItemType ItemType { get; init; }

    public Guid? CategoryProductionOrderId { get; init; }
    public Guid? CategoryAssignmentCodeId
    {
        get => _categoryAssignmentCodeId;
        init => _categoryAssignmentCodeId = value;
    }
    public Guid? CategoryEquipmentId
    {
        get => _categoryAssignmentCodeId;
        init => _categoryAssignmentCodeId = value;
    }
    public Guid? AdditionalCostProductionOrderId { get; init; }
    public Guid? AdditionalCostAssignmentCodeId
    {
        get => _additionalCostAssignmentCodeId;
        init => _additionalCostAssignmentCodeId = value;
    }
    public Guid? AdditionalCostEquipmentId
    {
        get => _additionalCostAssignmentCodeId;
        init => _additionalCostAssignmentCodeId = value;
    }

    public required List<IssuedDetailDto> IssuedDetails { get; init; }
    public required List<ShippedDetailDto> ShippedDetails { get; init; }

    // Vật tư tính vào doanh thu khoán
    public AcceptanceReportItemType? MaterialsIncludedInContractRevenueType { get; init; }
    public required MaterialsIncludedInContractRevenue MaterialsIncludedInContractRevenue { get; init; }
    public bool IsLongTermTracking { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public required double MaterialsIncludedInContractRevenueQuantity { get; init; }
    public List<AcceptanceReportCategoryAllocationDto>? CategoryAllocations { get; init; }

    // Bổ sung chi phí
    public AdditionalCost? AdditionalCostClassification { get; init; }
    public required AdditionalCost AdditionalCost { get; init; }
    public required OtherMaterialDetail OtherMaterialDetail { get; init; }
    public required double AdditionalCostQuantity { get; init; }

    // Vật tư theo hạn mức — chỉ cần khi QuotaBasedMaterial != None
    public required QuotaBasedMaterial QuotaBasedMaterial { get; init; }
    public required QuotaBasedMaterialType QuotaBasedMaterialType { get; init; }
    public List<QuotaBasedMaterialQuantityDto>? QuotaBasedMaterialQuantities { get; init; }

    // Tài sản
    public required Asset Asset { get; init; }
    public required double AssetMaterialQuantity { get; init; }
}

public record CreateAcceptanceReportDto
{
    public required Guid ProductionOutputId { get; init; }
    public required string FilePath { get; init; }
    public required List<CreateAcceptanceReportItemDto> Items { get; init; }
}

public record IssuedDetailDto
{
    public required IssuedQuantityType Type { get; init; }
    public required double Quantity { get; init; }
}

public record ShippedDetailDto
{
    public required ShippedQuantityType Type { get; init; }
    public required double Quantity { get; init; }
}

public record QuotaBasedMaterialQuantityDto
{
    public required QuotaBasedMaterialType Type { get; init; }
    public required double Quantity { get; init; }
}

public record CreateAcceptanceReportResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductionOutputId { get; set; }
    public string FilePath { get; set; }
    public int ItemCount { get; set; }
}
