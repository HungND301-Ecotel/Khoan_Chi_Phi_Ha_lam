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

    /// <summary>
    /// Id của Material nếu Type = Material, hoặc Id của Part nếu Type = Part
    /// </summary>
    public required Guid MaterialOrPartId { get; init; }

    public required string MaterialCode { get; init; }
    public required string UnitOfMeasureName { get; init; }
    public required AcceptanceReportItemType Type { get; init; }
    public required ItemType ItemType { get; init; }
    public required double IssuedQuantity { get; init; }
    public required double ShippedQuantity { get; init; }
}

public record UploadAcceptanceReportResponseDto
{
    public string FilePath { get; set; }
    public List<AcceptanceReportItemDto> AcceptanceReports { get; set; } = new();
}
