using Domain.Common.Enums;

namespace Application.Dto.Catalog.AcceptanceReport;

public record CreateAcceptanceReportItemDto
{
    /// <summary>
    /// Id của AcceptanceReportItem để update, null nếu là create
    /// </summary>
    public Guid? AcceptanceReportItemId { get; init; }

    /// <summary>
    /// Id của Material nếu Type = Material, hoặc Id của Part nếu Type = Part
    /// </summary>
    public required Guid MaterialOrPartId { get; init; }
    public required AcceptanceReportItemType Type { get; init; }

    public required double IssuedQuantity { get; init; }
    public required double ShippedQuantity { get; init; }

    // Vật tư tính vào doanh thu khoán
    public required MaterialsIncludedInContractRevenue MaterialsIncludedInContractRevenue { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public required double MaterialsIncludedInContractRevenueQuantity { get; init; }

    // Bổ sung chi phí
    public required AdditionalCost AdditionalCost { get; init; }
    public required double AdditionalCostQuantity { get; init; }

    // Vật tư theo hạn mức
    public required QuotaBasedMaterial QuotaBasedMaterial { get; init; }
    public required QuotaBasedMaterialType QuotaBasedMaterialType { get; init; }
    public required double QuotaBasedMaterialQuantity { get; init; }

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

public record CreateAcceptanceReportResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductionOutputId { get; set; }
    public string FilePath { get; set; }
    public int ItemCount { get; set; }
}
