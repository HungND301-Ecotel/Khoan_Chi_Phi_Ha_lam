namespace Application.Dto.Catalog.AcceptanceReport;

public record UpdateAcceptanceReportItemLogDto
{
    public required Guid Id { get; init; }
    public required Guid AcceptanceReportId { get; init; }
    public required double AllocationRatio { get; init; }
    public required bool IsFullAccounting { get; init; }
    public required string Note { get; init; }
}

public record UpdateAcceptanceReportItemLogResponseDto
{
    public Guid Id { get; init; }
    public double AllocationRatio { get; init; }
    public decimal ValueByStandard { get; init; }
    public decimal AccountedValueThisPeriod { get; init; }
    public decimal PendingValueEndPeriod { get; init; }
}
