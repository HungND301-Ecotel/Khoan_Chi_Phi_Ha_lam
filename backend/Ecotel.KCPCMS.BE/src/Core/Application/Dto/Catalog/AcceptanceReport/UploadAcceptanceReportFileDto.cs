using Domain.Common.Enums;
using Microsoft.AspNetCore.Http;

namespace Application.Dto.Catalog.AcceptanceReport;

public record UploadAcceptanceReportFileDto
{
    public required IFormFile File { get; init; }
    public required Guid ProductionOutputId { get; init; }
}

public record AcceptanceReportItemDto
{
    /// <summary>
    /// Id của AcceptanceReportItem (nếu update), null nếu tạo mới
    /// </summary>
    public Guid? ReportItemId { get; init; }

    public Guid? MaterialId { get; init; }
    public Guid? PartId { get; init; }

    public required string MaterialCode { get; init; }
    public required string UnitOfMeasureName { get; init; }
    public required AcceptanceReportItemType Type { get; init; }
    public required ItemType ItemType { get; init; }
    public PartType? PartType { get; init; }
    public required double IssuedQuantity { get; init; }
    public required double ShippedQuantity { get; init; }
}

public record UnresolvedAcceptanceReportItemDto
{
    public int RowNumber { get; init; }
    public Guid? ReportItemId { get; init; }
    public required string MaterialCode { get; init; }
    public string UnitOfMeasureName { get; init; } = string.Empty;
    public required double IssuedQuantity { get; init; }
    public required double ShippedQuantity { get; init; }
    public required string UnresolvedReason { get; init; }
}

public record UploadAcceptanceReportResponseDto
{
    public string FilePath { get; set; }
    public List<AcceptanceReportItemDto> AcceptanceReports { get; set; } = new();
    public List<UnresolvedAcceptanceReportItemDto> UnresolvedAcceptanceReports { get; set; } = new();
}
