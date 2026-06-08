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
    private Guid? _trackedMaterialId;
    private string? _trackedMaterialCode;
    private string? _trackedMaterialName;

    /// <summary>
    /// Id của AcceptanceReportItem (nếu update), null nếu tạo mới
    /// </summary>
    public Guid? ReportItemId { get; init; }
    public int RowNumber { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? PostingDate { get; init; }

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

    public required string MaterialCode { get; init; }
    public string? MaterialName { get; init; }
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
    public string? DocumentNumber { get; init; }
    public DateOnly? PostingDate { get; init; }
    public required string MaterialCode { get; init; }
    public string? MaterialName { get; init; }
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
