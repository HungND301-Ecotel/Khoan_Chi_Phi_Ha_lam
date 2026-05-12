using Domain.Entities.Production;

namespace Application.Catalog.Production.LongTermAnchorSeeds;

internal static class LongTermAnchorSeedTrackingHelper
{
    private const double Epsilon = 0.0001;

    internal record ReportContext(
        Guid AcceptanceReportId,
        DateOnly StartMonth,
        double ActualOutput,
        double StandardOutput,
        Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> OutputByProcessGroup);

    internal record TrackingSnapshot(
        Guid SeedItemId,
        Guid? AnchorSeedRowId,
        Guid PartId,
        Guid ProcessGroupId,
        string PartCode,
        string PartName,
        string UnitOfMeasureName,
        string ProcessGroupCode,
        string ProcessGroupName,
        decimal PendingValueStartPeriod,
        double IssuedQuantity,
        decimal UnitPrice,
        decimal TotalAmount,
        decimal OriginAmount,
        decimal TotalValueToAccount,
        double UsageTime,
        double AllocatedTime,
        double RemainingTime,
        double ActualOutput,
        double PlannedOutput,
        double StandardOutput,
        decimal ValueByStandard,
        double AllocationRatio,
        decimal AccountedValueThisPeriod,
        decimal PendingValueEndPeriod,
        string Note);

    internal static DateOnly? ResolveEffectiveMonth(IEnumerable<DateOnly> startMonths)
    {
        var earliest = startMonths.OrderBy(x => x).FirstOrDefault();
        return earliest == default ? null : earliest.AddMonths(-1);
    }

    internal static List<TrackingSnapshot> BuildSnapshots(
        IEnumerable<LongTermAnchorSeedItem> seedItems,
        IReadOnlyDictionary<Guid, (double PlannedOutput, double StandardOutput)> processGroupMetrics,
        IReadOnlyList<ReportContext> orderedReports,
        Guid targetAcceptanceReportId)
    {
        var targetIndex = orderedReports
            .Select((report, index) => new { report.AcceptanceReportId, Index = index })
            .FirstOrDefault(x => x.AcceptanceReportId == targetAcceptanceReportId)?.Index;

        if (!targetIndex.HasValue)
        {
            return [];
        }

        var snapshots = new List<TrackingSnapshot>();
        var reportsToEvaluate = orderedReports.Take(targetIndex.Value + 1).ToList();

        foreach (var seedItem in seedItems)
        {
            var currentPendingStart = seedItem.PendingValueStartPeriod;
            var currentAllocatedTime = seedItem.AllocatedTime;
            TrackingSnapshot? currentSnapshot = null;

            if (processGroupMetrics.TryGetValue(seedItem.ProcessGroupId, out var seedMetric))
            {
                var seedPeriodState = AdvancePeriod(
                    totalValueToAccount: seedItem.TotalValueToAccount,
                    usageTime: seedItem.UsageTime,
                    allocatedTime: currentAllocatedTime,
                    plannedOutput: seedMetric.PlannedOutput,
                    standardOutput: seedMetric.StandardOutput,
                    allocationRatio: seedItem.AllocationRatio);

                currentPendingStart = seedPeriodState.PendingValueEndPeriod;
                currentAllocatedTime = seedPeriodState.AllocatedTime;
            }
            else
            {
                currentPendingStart = seedItem.TotalValueToAccount;
            }

            for (var index = 0; index < reportsToEvaluate.Count; index++)
            {
                var report = reportsToEvaluate[index];
                var metrics = report.OutputByProcessGroup.TryGetValue(seedItem.ProcessGroupId, out var processMetrics)
                    ? processMetrics
                    : (
                        ActualOutput: report.ActualOutput,
                        PlannedOutput: report.ActualOutput,
                        StandardOutput: report.StandardOutput);

                if (metrics.PlannedOutput <= 0 && processGroupMetrics.TryGetValue(seedItem.ProcessGroupId, out var processGroupMetric))
                {
                    metrics.PlannedOutput = processGroupMetric.PlannedOutput;
                }

                if (metrics.StandardOutput <= 0 && processGroupMetrics.TryGetValue(seedItem.ProcessGroupId, out processGroupMetric))
                {
                    metrics.StandardOutput = processGroupMetric.StandardOutput;
                }

                var issuedQuantity = 0d;
                var unitPrice = 0m;
                var totalAmount = 0m;
                var totalValueToAccount = currentPendingStart + totalAmount;
                var displayAllocatedTime = Math.Min(seedItem.UsageTime, currentAllocatedTime);
                var displayRemainingTime = Math.Max(0, seedItem.UsageTime - displayAllocatedTime);

                if (totalValueToAccount <= 0)
                {
                    currentSnapshot = null;
                    break;
                }

                var periodState = AdvancePeriod(
                    totalValueToAccount,
                    seedItem.UsageTime,
                    currentAllocatedTime,
                    metrics.PlannedOutput,
                    metrics.StandardOutput,
                    seedItem.AllocationRatio);

                if (periodState.RemainingTime < -Epsilon)
                {
                    currentSnapshot = null;
                    break;
                }

                currentSnapshot = new TrackingSnapshot(
                    seedItem.Id,
                    seedItem.AnchorSeedRowId,
                    seedItem.PartId,
                    seedItem.ProcessGroupId,
                    seedItem.Part.Code?.Value ?? string.Empty,
                    seedItem.Part.Name,
                    seedItem.Part.UnitOfMeasure?.Name ?? string.Empty,
                    seedItem.ProcessGroup.Code?.Value ?? string.Empty,
                    seedItem.ProcessGroup.Name,
                    currentPendingStart,
                    issuedQuantity,
                    unitPrice,
                    totalAmount,
                    seedItem.OriginAmount,
                    totalValueToAccount,
                    seedItem.UsageTime,
                    displayAllocatedTime,
                    displayRemainingTime,
                    metrics.ActualOutput,
                    metrics.PlannedOutput,
                    metrics.StandardOutput,
                    periodState.ValueByStandard,
                    periodState.AllocationRatio,
                    periodState.AccountedValueThisPeriod,
                    periodState.PendingValueEndPeriod,
                    seedItem.Note);

                currentPendingStart = periodState.PendingValueEndPeriod;
                currentAllocatedTime = periodState.AllocatedTime;
            }

            if (currentSnapshot != null && currentSnapshot.TotalValueToAccount > 0)
            {
                snapshots.Add(currentSnapshot);
            }
        }

        return snapshots
            .OrderBy(x => x.ProcessGroupCode)
            .ThenBy(x => x.PartCode)
            .ToList();
    }

    private static (decimal ValueByStandard, double AllocationRatio, decimal AccountedValueThisPeriod, decimal PendingValueEndPeriod, double AllocatedTime, double RemainingTime) AdvancePeriod(
        decimal totalValueToAccount,
        double usageTime,
        double allocatedTime,
        double plannedOutput,
        double standardOutput,
        double allocationRatio)
    {
        var remainingTime = usageTime - allocatedTime;
        if (remainingTime < -Epsilon)
        {
            return (0, allocationRatio, 0, 0, allocatedTime, remainingTime);
        }

        var normalizedAllocationRatio = allocationRatio;
        var valueByStandard = usageTime > 0 && standardOutput > 0
            ? (totalValueToAccount / (decimal)usageTime) * ((decimal)plannedOutput / (decimal)standardOutput)
            : 0;

        decimal accountedValueThisPeriod;
        decimal pendingValueEndPeriod;
        double displayAllocatedTime;
        double displayRemainingTime;

        if (Math.Abs(remainingTime) < Epsilon)
        {
            normalizedAllocationRatio = normalizedAllocationRatio == 0 ? 1 : normalizedAllocationRatio;
            valueByStandard = totalValueToAccount;
            accountedValueThisPeriod = totalValueToAccount;
            pendingValueEndPeriod = 0;
            displayAllocatedTime = usageTime;
            displayRemainingTime = 0;
        }
        else
        {
            accountedValueThisPeriod = Math.Min(totalValueToAccount, valueByStandard * (decimal)normalizedAllocationRatio);
            pendingValueEndPeriod = totalValueToAccount - accountedValueThisPeriod;
            displayAllocatedTime = Math.Min(usageTime, allocatedTime + normalizedAllocationRatio);
            displayRemainingTime = Math.Max(0, usageTime - displayAllocatedTime);
        }

        return (
            valueByStandard,
            normalizedAllocationRatio,
            accountedValueThisPeriod,
            pendingValueEndPeriod,
            displayAllocatedTime,
            displayRemainingTime);
    }
}
