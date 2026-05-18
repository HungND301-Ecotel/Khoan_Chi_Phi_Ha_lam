using Domain.Common.Enums;

namespace Application.Dto.Catalog.AcceptanceReport;

public record AcceptanceReportCategoryAllocationDetailDto
{
    public required Guid ProcessGroupId { get; init; }
    public string? ProcessGroupCode { get; init; }
    public string? ProcessGroupName { get; init; }
    public required double Quantity { get; init; }
    public List<Guid> EquipmentIds { get; init; } = new();
}

public record AcceptanceReportDetailItemDto
{
    public required Guid Id { get; init; }
    public required Guid AcceptanceReportId { get; init; }
    public required Guid? CategoryProductionOrderId { get; init; }
    public Guid? CategoryEquipmentId { get; init; }
    public Guid? AdditionalCostProductionOrderId { get; init; }
    public Guid? AdditionalCostEquipmentId { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? PartId { get; init; }
    public Guid? TrackedMaterialId { get; init; }
    public double UsageTime { get; init; }
    public string? MaterialCode { get; init; }
    public string? MaterialName { get; init; }
    public string? PartCode { get; init; }
    public string? PartName { get; init; }
    public string? TrackedMaterialCode { get; init; }
    public string? TrackedMaterialName { get; init; }
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
    public required MaterialsIncludedInContractRevenue MaterialsIncludedInContractRevenue { get; init; }
    public required bool IsLongTermTracking { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public string? ProcessGroupCode { get; init; }
    public string? ProcessGroupName { get; init; }
    public required double MaterialsIncludedInContractRevenueQuantity { get; init; }
    public List<AcceptanceReportCategoryAllocationDetailDto> CategoryAllocations { get; init; } = new();

    // Bổ sung chi phí
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

