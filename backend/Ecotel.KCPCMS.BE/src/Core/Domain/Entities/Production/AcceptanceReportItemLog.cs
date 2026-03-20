using Domain.Common.Contracts;

namespace Domain.Entities.Production;

public class AcceptanceReportItemLog : AuditableEntity<Guid>
{
    // Tracking keys
    public Guid AcceptanceReportItemId { get; protected set; }
    public Guid AcceptanceReportId { get; protected set; }

    // Period information
    public DateOnly PeriodStartMonth { get; protected set; }
    public DateOnly PeriodEndMonth { get; protected set; }

    // Financial values
    public decimal PendingValueStartPeriod { get; protected set; }
    public double IssuedQuantity { get; protected set; }
    public decimal UnitPrice { get; protected set; }
    public decimal TotalAmount { get; protected set; }
    public decimal OriginAmount { get; protected set; }
    public bool IsFullAccounting { get; protected set; }
    public decimal TotalValueToAccount { get; protected set; }

    // Time tracking
    public double UsageTime { get; protected set; }
    public double AllocatedTime { get; protected set; }
    public double RemainingTime { get; protected set; }

    // Output tracking
    public double ActualOutput { get; protected set; }
    public double PlannedOutput { get; protected set; }
    public double StandardOutput { get; protected set; }

    // Calculation values
    public decimal ValueByStandard { get; protected set; }
    public double AllocationRatio { get; protected set; }
    public decimal AccountedValueThisPeriod { get; protected set; }
    public decimal PendingValueEndPeriod { get; protected set; }

    public string Note { get; protected set; } = "";

    // Navigation properties
    public virtual AcceptanceReportItem AcceptanceReportItem { get; protected set; }
    public virtual AcceptanceReport AcceptanceReport { get; protected set; }
    public virtual AcceptanceReportItemLog? PreviousLog { get; protected set; }

    public static AcceptanceReportItemLog Create(
        Guid acceptanceReportItemId,
        Guid acceptanceReportId,
        DateOnly periodStartMonth,
        DateOnly periodEndMonth,
        decimal pendingValueStartPeriod,
        double issuedQuantity,
        decimal unitPrice,
        double usageTime,
        double allocatedTime,
        double actualOutput,
        double plannedOutput,
        double standardOutput,
        double allocationRatio,
        bool isFullAccounting = false,
        string note = "")
    {
        var log = new AcceptanceReportItemLog
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            AcceptanceReportId = acceptanceReportId,
            PeriodStartMonth = periodStartMonth,
            PeriodEndMonth = periodEndMonth,
            PendingValueStartPeriod = pendingValueStartPeriod,
            IssuedQuantity = issuedQuantity,
            UnitPrice = unitPrice,
            UsageTime = usageTime,
            AllocatedTime = allocatedTime,
            ActualOutput = actualOutput,
            PlannedOutput = plannedOutput,
            StandardOutput = standardOutput,
            AllocationRatio = allocationRatio,
            IsFullAccounting = isFullAccounting,
            Note = note
        };

        log.Calculate();

        if (unitPrice > 0)
        {
            log.OriginAmount = log.TotalAmount;
        }
        return log;
    }

    private void Calculate()
    {
        // Thành tiền = Số lượng lĩnh * đơn giá
        TotalAmount = (decimal)IssuedQuantity * UnitPrice;

        // TỔNG GIÁ TRỊ CẦN HẠCH TOÁN (Nguyên giá)
        TotalValueToAccount = PendingValueStartPeriod + TotalAmount;

        // Thời gian còn lại
        RemainingTime = UsageTime - AllocatedTime;

        // GIÁ TRỊ CẦN HẠCH TOÁN THEO ĐỊNH MỨC
        if (UsageTime > 0 && StandardOutput > 0)
        {
            ValueByStandard = (TotalValueToAccount / (decimal)UsageTime) * (decimal)ActualOutput / (decimal)StandardOutput;
        }
        else
        {
            ValueByStandard = 0;
        }

        // Nếu RemainingTime = 0 → Kỳ cuối, hạch toán hết
        if (Math.Abs(RemainingTime) < 0.0001 || IsFullAccounting)
        {
            ValueByStandard = TotalValueToAccount;
            AccountedValueThisPeriod = TotalValueToAccount;
            PendingValueEndPeriod = 0;
            RemainingTime = 0;
            AllocatedTime = UsageTime;
        }
        else
        {
            // GIÁ TRỊ DÀI KỲ HẠCH TOÁN KỲ NÀY
            AccountedValueThisPeriod = Math.Min(TotalValueToAccount, ValueByStandard * (decimal)AllocationRatio);

            // GIÁ TRỊ CUỐI KỲ CHỜ HẠCH TOÁN KỲ SAU
            PendingValueEndPeriod = TotalValueToAccount - AccountedValueThisPeriod;
        }
    }

    public void UpdateAllocationRatio(double allocationRatio, bool isFullAccounting, string note = "")
    {
        AllocationRatio = allocationRatio;
        IsFullAccounting = isFullAccounting;
        Note = note;
        Calculate();
    }

    public void UpdatePlannedOutput(double plannedOutput, string note = "")
    {
        PlannedOutput = plannedOutput;
        Note = note;
        Calculate();
    }
    public void UpdatePendingValueStartPeriod(decimal pendingValueStartPeriod)
    {
        PendingValueStartPeriod = pendingValueStartPeriod;
        Calculate();
    }
}
