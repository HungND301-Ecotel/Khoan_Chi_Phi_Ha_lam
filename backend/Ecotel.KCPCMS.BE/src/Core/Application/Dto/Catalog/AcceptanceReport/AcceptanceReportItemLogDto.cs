namespace Application.Dto.Catalog.AcceptanceReport;

public record AcceptanceReportItemLogDto
{
    public Guid Id { get; init; }
    public Guid AcceptanceReportItemId { get; init; }
    public Guid? MaterialId { get; init; }
    public Guid? TrackedMaterialId { get; init; }
    public Guid? ProcessGroupId { get; init; }
    public string? ProcessGroupCode { get; init; }
    public string? ProcessGroupName { get; init; }
    public string? PartCode { get; init; }
    public string? PartName { get; init; }
    public string? MaterialCode { get; init; }
    public string? MaterialName { get; init; }
    public string? TrackedMaterialCode { get; init; }
    public string? TrackedMaterialName { get; init; }
    public string? UnitOfMeasureName { get; init; }

    // Financial values
    public decimal PendingValueStartPeriod { get; init; }
    public double IssuedQuantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal OriginAmount { get; init; }
    public decimal TotalValueToAccount { get; init; }

    // Time tracking
    public double UsageTime { get; init; }
    public double AllocatedTime { get; init; }
    public double RemainingTime { get; init; }

    // Output tracking
    public double ActualOutput { get; init; }
    public double PlannedOutput { get; init; }
    public double StandardOutput { get; init; }

    // Calculation values
    public decimal ValueByStandard { get; init; }
    public double AllocationRatio { get; init; }
    public decimal AccountedValueThisPeriod { get; init; }
    public decimal PendingValueEndPeriod { get; init; }

    // Status
    public bool IsNewItem { get; init; } // TH1 or TH2
    public bool IsFullAccounting { get; init; }


    // Additional info
    public string? Note { get; init; }
    public bool IsAnchorSeed { get; init; }
}

public record AcceptanceReportItemLogProcessGroupDto
{
    public Guid ProcessGroupId { get; init; }
    public string ProcessGroupCode { get; init; } = string.Empty;
    public string ProcessGroupName { get; init; } = string.Empty;
    public List<AcceptanceReportItemLogDto> Items { get; init; } = new();
}

public record GetAllAcceptanceReportItemLogResponseDto
{
    public Guid AcceptanceReportId { get; init; }
    public DateOnly PeriodStartMonth { get; init; }
    public DateOnly PeriodEndMonth { get; init; }
    public List<AcceptanceReportItemLogDto> Items { get; init; } = new();
    public List<AcceptanceReportItemLogProcessGroupDto> ProcessGroups { get; init; } = new();
}
