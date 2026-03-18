namespace Application.Dto.Catalog.AcceptanceReport;

public record DetailLongTermTrackingItemDto
{
    public Guid Id { get; init; }
    public Guid AcceptanceReportItemId { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public string? ProcessGroupCode { get; init; }
    public string? ProcessGroupName { get; init; }
    public string? PartCode { get; init; }
    public string? PartName { get; init; }
    public string? UnitOfMeasureName { get; init; }

    // Financial values
    public decimal PendingValueStartPeriod { get; init; }
    public double IssuedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalValueToAccount { get; init; }

    // Time tracking
    public double UsageTime { get; init; }
    public double AllocatedTime { get; init; }
    public double RemainingTime { get; init; }

    // Calculation values
    public decimal ValueByStandard { get; init; }
    public double AllocationRatio { get; init; }
    public decimal AccountedValueThisPeriod { get; init; }
    public decimal PendingValueEndPeriod { get; init; }

    // Additional info
    public string? Note { get; init; }
    public double ActualOutput { get; init; }
    public double PlannedOutput { get; init; }
    public double StandardOutput { get; init; }
    public bool IsNewItem { get; init; }
}

public record DetailLongTermTrackingProcessGroupDto
{
    public Guid ProcessGroupId { get; init; }
    public string ProcessGroupCode { get; init; } = string.Empty;
    public string ProcessGroupName { get; init; } = string.Empty;
    public List<DetailLongTermTrackingItemDto> Items { get; init; } = new();
}

public record GetDetailLongTermTrackingResponseDto
{
    public Guid AcceptanceReportId { get; init; }
    public List<DetailLongTermTrackingItemDto> Items { get; init; } = new();
    public List<DetailLongTermTrackingProcessGroupDto> ProcessGroups { get; init; } = new();
}
