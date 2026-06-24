using Domain.Entities.Production;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

internal static class AcceptanceReportItemLogCommandHelper
{
    internal static Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> BuildOutputByProcessGroup(
        ProductionOutput productionOutput)
    {
        var result = new Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)>();

        foreach (var processGroup in productionOutput.ProductionOutputProcessGroups)
        {
            result[processGroup.ProcessGroupId] = (
                processGroup.ProductionMeters,
                processGroup.PlanProductionMeters,
                processGroup.StandardProductionMeters);
        }

        return result;
    }

    internal static bool IsNewItemInCurrentPeriod(
        AcceptanceReportItemLog log,
        Guid currentAcceptanceReportId)
        => log.AcceptanceReportId == currentAcceptanceReportId
            && (log.IssuedQuantity > 0 || log.TotalAmount > 0 || log.PendingValueStartPeriod == 0);

    internal static void EnsureUsageTimeCanBeUpdated(
        AcceptanceReportItemLog log,
        Guid currentAcceptanceReportId,
        double requestedUsageTime)
    {
        if (requestedUsageTime < 0)
        {
            throw new Application.Common.Exceptions.BadRequestException(
                "Thời gian sử dụng không được âm");
        }
    }

    internal static void RefreshLogOutputMetrics(
        AcceptanceReportItemLog log,
        IReadOnlyDictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> outputByProcessGroup,
        string note)
    {
        var processGroupId = ResolveProcessGroupId(log);
        if (!processGroupId.HasValue || !outputByProcessGroup.TryGetValue(processGroupId.Value, out var metrics))
        {
            return;
        }

        log.UpdateOutputMetrics(metrics.ActualOutput, metrics.PlannedOutput, metrics.StandardOutput, note);
    }

    internal static AcceptanceReportItemLog CreateOverrideLog(
        AcceptanceReportItemLog sourceLog,
        Guid acceptanceReportId,
        ProductionOutput productionOutput,
        IReadOnlyDictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> outputByProcessGroup,
        double usageTime,
        double allocationRatio,
        bool isFullAccounting,
        string note,
        double totalAllocatedTime)
    {
        var actualOutput = productionOutput.ProductionMeters;
        var plannedOutput = sourceLog.PlannedOutput;
        var standardOutput = productionOutput.StandardProductionMeters;

        var processGroupId = ResolveProcessGroupId(sourceLog);
        if (processGroupId.HasValue &&
            outputByProcessGroup.TryGetValue(processGroupId.Value, out var metrics))
        {
            actualOutput = metrics.ActualOutput;
            plannedOutput = metrics.PlannedOutput;
            standardOutput = metrics.StandardOutput;
        }

        return AcceptanceReportItemLog.Create(
            acceptanceReportItemId: sourceLog.AcceptanceReportItemId,
            acceptanceReportId: acceptanceReportId,
            periodStartMonth: productionOutput.StartMonth,
            periodEndMonth: productionOutput.EndMonth,
            pendingValueStartPeriod: sourceLog.PendingValueEndPeriod,
            issuedQuantity: 0,
            unitPrice: 0,
            usageTime: usageTime,
            allocatedTime: totalAllocatedTime,
            actualOutput: actualOutput,
            plannedOutput: plannedOutput,
            standardOutput: standardOutput,
            allocationRatio: allocationRatio,
            acceptanceReportItemCategoryAllocationId: sourceLog.AcceptanceReportItemCategoryAllocationId,
            isFullAccounting: isFullAccounting,
            note: note);
    }

    internal static double ResolveFinalAllocationRatio(
        AcceptanceReportItemLog log,
        double requestedAllocationRatio,
        double totalAllocatedTime)
    {
        var remainingTime = log.UsageTime - totalAllocatedTime;
        return Math.Abs(remainingTime) < 0.0001 && requestedAllocationRatio == 0
            ? 1.0
            : requestedAllocationRatio;
    }

    private static Guid? ResolveProcessGroupId(AcceptanceReportItemLog log)
        => log.AcceptanceReportItemCategoryAllocation?.ProcessGroupId ?? log.AcceptanceReportItem?.ProcessGroupId;
}
