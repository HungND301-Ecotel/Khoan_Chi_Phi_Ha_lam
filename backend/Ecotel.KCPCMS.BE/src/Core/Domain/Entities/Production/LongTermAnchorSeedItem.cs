using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class LongTermAnchorSeedItem : AuditableEntity<Guid>
{
    public Guid LongTermAnchorSeedId { get; protected set; }
    public Guid? AnchorSeedRowId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public Guid PartId { get; protected set; }
    public Guid? AssignmentCodeId { get; protected set; }
    public Guid? ProductionOrderId { get; protected set; }
    public Guid MaterialId => PartId;
    public Guid TrackedMaterialId => MaterialId;
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
    public virtual Material Part { get; protected set; } = default!;
    public virtual AssignmentCode? AssignmentCode { get; protected set; }
    public virtual ProductionOrder? ProductionOrder { get; protected set; }
    public Material Material => Part;

    public decimal TotalAmount => (decimal)IssuedQuantity * UnitPrice;
    public decimal OriginAmount => TotalAmount > 0 ? TotalAmount : PendingValueStartPeriod;
    public decimal TotalValueToAccount => PendingValueStartPeriod + TotalAmount;
    public double RemainingTime => UsageTime - AllocatedTime;

    public static LongTermAnchorSeedItem Create(
        Guid longTermAnchorSeedId,
        Guid processGroupId,
        Guid partId,
        Guid? assignmentCodeId,
        Guid? productionOrderId,
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
            AssignmentCodeId = assignmentCodeId,
            ProductionOrderId = productionOrderId,
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

    public static LongTermAnchorSeedItem CreateForMaterial(
        Guid longTermAnchorSeedId,
        Guid processGroupId,
        Guid materialId,
        Guid? assignmentCodeId,
        Guid? productionOrderId,
        int sortOrder,
        double issuedQuantity,
        decimal unitPrice,
        decimal pendingValueStartPeriod,
        double usageTime,
        double allocatedTime,
        double allocationRatio,
        string? note,
        Guid? anchorSeedRowId = null)
        => Create(
            longTermAnchorSeedId,
            processGroupId,
            materialId,
            assignmentCodeId,
            productionOrderId,
            sortOrder,
            issuedQuantity,
            unitPrice,
            pendingValueStartPeriod,
            usageTime,
            allocatedTime,
            allocationRatio,
            note,
            anchorSeedRowId);

    public void Update(
        Guid processGroupId,
        Guid partId,
        Guid? assignmentCodeId,
        Guid? productionOrderId,
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
        AssignmentCodeId = assignmentCodeId;
        ProductionOrderId = productionOrderId;
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

    public void UpdateForMaterial(
        Guid processGroupId,
        Guid materialId,
        Guid? assignmentCodeId,
        Guid? productionOrderId,
        int sortOrder,
        double issuedQuantity,
        decimal unitPrice,
        decimal pendingValueStartPeriod,
        double usageTime,
        double allocatedTime,
        double allocationRatio,
        string? note)
        => Update(
            processGroupId,
            materialId,
            assignmentCodeId,
            productionOrderId,
            sortOrder,
            issuedQuantity,
            unitPrice,
            pendingValueStartPeriod,
            usageTime,
            allocatedTime,
            allocationRatio,
            note);

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

        if (pendingValueStartPeriod <= 0)
        {
            throw new ArgumentException("Phải nhập tổng giá trị cần hạch toán lớn hơn 0");
        }
    }
}
