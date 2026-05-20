using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class LongTermAnchorSeedItem : AuditableEntity<Guid>
{
    public Guid LongTermAnchorSeedId { get; protected set; }
    public Guid? AnchorSeedRowId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public Guid PartId { get; protected set; }
    public Guid MaterialId => PartId;
    public int SortOrder { get; protected set; }
    public double IssuedQuantity { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal PendingValueStartPeriod { get; protected set; }
    public double UsageTime { get; protected set; }
    public double AllocatedTime { get; protected set; }
    public double AllocationRatio { get; protected set; }
    public string Note { get; protected set; } = string.Empty;

    public virtual LongTermAnchorSeed LongTermAnchorSeed { get; protected set; } = default!;
    public virtual ProcessGroup ProcessGroup { get; protected set; } = default!;
    public virtual Part Part { get; protected set; } = default!;
    public Part Material => Part;

    public decimal TotalAmount => (decimal)IssuedQuantity * UnitPrice;
    public decimal OriginAmount => TotalAmount > 0 ? TotalAmount : PendingValueStartPeriod;
    public decimal TotalValueToAccount => PendingValueStartPeriod + TotalAmount;
    public double RemainingTime => UsageTime - AllocatedTime;

    public static LongTermAnchorSeedItem Create(
        Guid longTermAnchorSeedId,
        Guid processGroupId,
        Guid partId,
        int sortOrder,
        double issuedQuantity,
        decimal unitPrice,
        decimal pendingValueStartPeriod,
        double usageTime,
        double allocatedTime,
        double allocationRatio,
        string? note,
        Guid? anchorSeedRowId = null)
    {
        Validate(issuedQuantity, unitPrice, pendingValueStartPeriod, usageTime, allocatedTime, allocationRatio);

        return new LongTermAnchorSeedItem
        {
            LongTermAnchorSeedId = longTermAnchorSeedId,
            AnchorSeedRowId = anchorSeedRowId ?? Guid.NewGuid(),
            ProcessGroupId = processGroupId,
            PartId = partId,
            SortOrder = sortOrder,
            IssuedQuantity = issuedQuantity,
            UnitPrice = unitPrice,
            PendingValueStartPeriod = pendingValueStartPeriod,
            UsageTime = usageTime,
            AllocatedTime = allocatedTime,
            AllocationRatio = allocationRatio,
            Note = note ?? string.Empty
        };
    }

    public void Update(
        Guid processGroupId,
        Guid partId,
        int sortOrder,
        double issuedQuantity,
        decimal unitPrice,
        decimal pendingValueStartPeriod,
        double usageTime,
        double allocatedTime,
        double allocationRatio,
        string? note)
    {
        Validate(issuedQuantity, unitPrice, pendingValueStartPeriod, usageTime, allocatedTime, allocationRatio);

        ProcessGroupId = processGroupId;
        PartId = partId;
        SortOrder = sortOrder;
        IssuedQuantity = issuedQuantity;
        UnitPrice = unitPrice;
        PendingValueStartPeriod = pendingValueStartPeriod;
        UsageTime = usageTime;
        AllocatedTime = allocatedTime;
        AllocationRatio = allocationRatio;
        Note = note ?? string.Empty;

        if (!AnchorSeedRowId.HasValue)
        {
            AnchorSeedRowId = Guid.NewGuid();
        }
    }

    private static void Validate(
        double issuedQuantity,
        decimal unitPrice,
        decimal pendingValueStartPeriod,
        double usageTime,
        double allocatedTime,
        double allocationRatio)
    {
        if (issuedQuantity < 0)
        {
            throw new ArgumentException("Số lượng không được âm");
        }

        if (unitPrice < 0)
        {
            throw new ArgumentException("Đơn giá không được âm");
        }

        if (pendingValueStartPeriod < 0)
        {
            throw new ArgumentException("Giá trị chờ hạch toán đầu kỳ không được âm");
        }

        if (usageTime < 0)
        {
            throw new ArgumentException("Thời gian sử dụng không được âm");
        }

        if (allocatedTime < 0)
        {
            throw new ArgumentException("Thời gian đã phân bổ không được âm");
        }

        if (allocationRatio < 0)
        {
            throw new ArgumentException("Tỷ lệ phân bổ không được âm");
        }

        var hasPendingValueStartPeriod = pendingValueStartPeriod > 0;
        var hasIssuedQuantity = issuedQuantity > 0;
        var hasUnitPrice = unitPrice > 0;

        if (!hasPendingValueStartPeriod && hasIssuedQuantity != hasUnitPrice)
        {
            throw new ArgumentException("Phải nhập đồng thời số lượng và đơn giá khi không nhập giá trị chờ hạch toán đầu kỳ");
        }

        if (hasPendingValueStartPeriod && (hasIssuedQuantity || hasUnitPrice))
        {
            throw new ArgumentException("Không được nhập đồng thời giá trị chờ hạch toán đầu kỳ với số lượng hoặc đơn giá");
        }

        if (!hasPendingValueStartPeriod && !hasIssuedQuantity && !hasUnitPrice)
        {
            throw new ArgumentException("Phải nhập giá trị chờ hạch toán đầu kỳ hoặc đồng thời số lượng và đơn giá");
        }
    }
}
