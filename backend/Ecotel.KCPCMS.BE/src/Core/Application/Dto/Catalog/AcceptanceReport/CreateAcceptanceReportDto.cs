using Domain.Common.Enums;

namespace Application.Dto.Catalog.AcceptanceReport;

public record CreateAcceptanceReportItemDto
{
    public Guid? AcceptanceReportItemId { get; init; }

    public Guid? MaterialId { get; init; }
    public Guid? PartId { get; init; }
    public Guid? MaintainUnitPriceEquipmentId { get; init; }

    public required AcceptanceReportItemType Type { get; init; }
    public required ItemType ItemType { get; init; }

    public Guid? CategoryProductionOrderId { get; init; }
    public Guid? CategoryEquipmentId { get; init; }
    public Guid? AdditionalCostProductionOrderId { get; init; }
    public Guid? AdditionalCostEquipmentId { get; init; }

    public required List<IssuedDetailDto> IssuedDetails { get; init; }
    public required List<ShippedDetailDto> ShippedDetails { get; init; }

    // Vật tư tính vào doanh thu khoán
    public Guid? MaterialsIncludedInContractRevenueFixedKeyId { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public required double MaterialsIncludedInContractRevenueQuantity { get; init; }

    // Bổ sung chi phí
    public Guid? AdditionalCostFixedKeyId { get; init; }
    public Guid? OtherMaterialDetailFixedKeyId { get; init; }
    public required double AdditionalCostQuantity { get; init; }

    // Vật tư theo hạn mức — chỉ cần khi QuotaBasedMaterial != None
    public Guid? QuotaBasedMaterialFixedKeyId { get; init; }
    public Guid? QuotaBasedMaterialTypeFixedKeyId { get; init; }
    public List<QuotaBasedMaterialQuantityDto>? QuotaBasedMaterialQuantities { get; init; }

    // Tài sản
    public Guid? AssetFixedKeyId { get; init; }
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
    public IssuedQuantityType? Type { get; init; }
    public Guid? FixedKeyId { get; init; }
    public required double Quantity { get; init; }
}

public record ShippedDetailDto
{
    public ShippedQuantityType? Type { get; init; }
    public Guid? FixedKeyId { get; init; }
    public required double Quantity { get; init; }
}

public record QuotaBasedMaterialQuantityDto
{
    public QuotaBasedMaterialType? Type { get; init; }
    public Guid? FixedKeyId { get; init; }
    public required double Quantity { get; init; }
}

public record CreateAcceptanceReportResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductionOutputId { get; set; }
    public string FilePath { get; set; }
    public int ItemCount { get; set; }
}
