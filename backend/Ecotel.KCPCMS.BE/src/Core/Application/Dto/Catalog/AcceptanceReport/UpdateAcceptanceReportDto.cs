using Domain.Common.Enums;

namespace Application.Dto.Catalog.AcceptanceReport;

public record UpdateAcceptanceReportItemDto
{
    public required Guid Id { get; init; }

    public required List<IssuedDetailDto> IssuedDetails { get; init; }
    public required List<ShippedDetailDto> ShippedDetails { get; init; }

    public required ItemType ItemType { get; init; }
    public Guid? CategoryProductionOrderId { get; init; }
    public Guid? CategoryEquipmentId { get; init; }
    public Guid? AdditionalCostProductionOrderId { get; init; }
    public Guid? AdditionalCostEquipmentId { get; init; }

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

public record UpdateAcceptanceReportDto
{
    public required Guid Id { get; init; }
    public required string FilePath { get; init; }
    public required List<UpdateAcceptanceReportItemDto> Items { get; init; }
}

public record UpdateAcceptanceReportResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductionOutputId { get; set; }
    public string FilePath { get; set; }
    public int ItemCount { get; set; }
}
