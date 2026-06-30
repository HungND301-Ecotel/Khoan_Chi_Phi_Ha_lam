using Domain.Common.Contracts;

namespace Domain.Entities.Production;

public class LongTermAnchorSeedItemLog : AuditableEntity<Guid>
{
    public Guid LongTermAnchorSeedItemId { get; protected set; }
    public Guid AcceptanceReportId { get; protected set; }
    public DateOnly PeriodStartMonth { get; protected set; }
    public DateOnly PeriodEndMonth { get; protected set; }
    public decimal PendingValueStartPeriod { get; protected set; }
    public double IssuedQuantity { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public decimal OriginAmount { get; protected set; }
    public decimal TotalValueToAccount { get; protected set; }
    public double UsageTime { get; protected set; }
    public double AllocatedTime { get; protected set; }
    public double RemainingTime { get; protected set; }
    public double ActualOutput { get; protected set; }
    public double PlannedOutput { get; protected set; }
    public double StandardOutput { get; protected set; }
    public decimal ValueByStandard { get; protected set; }
    public double AllocationRatio { get; protected set; }
    public decimal AccountedValueThisPeriod { get; protected set; }
    public decimal PendingValueEndPeriod { get; protected set; }
    public string Note { get; protected set; } = string.Empty;

    public virtual LongTermAnchorSeedItem LongTermAnchorSeedItem { get; protected set; } = default!;
    public virtual AcceptanceReport AcceptanceReport { get; protected set; } = default!;

    public static LongTermAnchorSeedItemLog Create(
        Guid longTermAnchorSeedItemId,
        Guid acceptanceReportId,
        DateOnly periodStartMonth,
        DateOnly periodEndMonth,
        decimal pendingValueStartPeriod,
        double issuedQuantity,
        decimal unitPrice,
        decimal totalAmount,
        decimal originAmount,
        decimal totalValueToAccount,
        double usageTime,
        double allocatedTime,
        double remainingTime,
        double actualOutput,
        double plannedOutput,
        double standardOutput,
        decimal valueByStandard,
        double allocationRatio,
        decimal accountedValueThisPeriod,
        decimal pendingValueEndPeriod,
        string? note)
    {
        return new LongTermAnchorSeedItemLog
        {
            LongTermAnchorSeedItemId = longTermAnchorSeedItemId,
            AcceptanceReportId = acceptanceReportId,
            PeriodStartMonth = periodStartMonth,
            PeriodEndMonth = periodEndMonth,
            PendingValueStartPeriod = pendingValueStartPeriod,
            IssuedQuantity = issuedQuantity,
            UnitPrice = unitPrice,
            TotalAmount = totalAmount,
            OriginAmount = originAmount,
            TotalValueToAccount = totalValueToAccount,
            UsageTime = usageTime,
            AllocatedTime = allocatedTime,
            RemainingTime = remainingTime,
            ActualOutput = actualOutput,
            PlannedOutput = plannedOutput,
            StandardOutput = standardOutput,
            ValueByStandard = valueByStandard,
            AllocationRatio = allocationRatio,
            AccountedValueThisPeriod = accountedValueThisPeriod,
            PendingValueEndPeriod = pendingValueEndPeriod,
            Note = note ?? string.Empty
        };
    }
}
