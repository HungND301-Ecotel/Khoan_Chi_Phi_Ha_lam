using Domain.Entities.Production;

namespace Application.Catalog.Production.LongTermAnchorSeeds;

internal static class LongTermAnchorSeedLogPersistenceHelper
{
    internal static List<LongTermAnchorSeedItemLog> BuildLogs(
        LongTermAnchorSeed seed,
        IEnumerable<AcceptanceReport> reports)
    {
        var orderedReports = reports
            .Where(report => report.ProductionOutput != null)
            .OrderBy(report => report.ProductionOutput!.StartMonth)
            .ThenBy(report => report.CreatedOn)
            .ToList();

        if (!seed.Items.Any() || orderedReports.Count == 0)
        {
            return [];
        }

        var processGroupMetrics = seed.ProcessGroupMetrics
            .GroupBy(x => x.ProcessGroupId)
            .ToDictionary(
                x => x.Key,
                x => (
                    PlannedOutput: x.First().PlannedOutput,
                    StandardOutput: x.First().StandardOutput));

        var reportContexts = orderedReports
            .Select(report => new LongTermAnchorSeedTrackingHelper.ReportContext(
                report.Id,
                report.ProductionOutput!.StartMonth,
                report.ProductionOutput.ProductionMeters,
                report.ProductionOutput.StandardProductionMeters,
                BuildOutputByProcessGroup(report.ProductionOutput)))
            .ToList();

        var logs = new List<LongTermAnchorSeedItemLog>();

        foreach (var report in orderedReports)
        {
            var snapshots = LongTermAnchorSeedTrackingHelper.BuildSnapshots(
                seed.Items,
                processGroupMetrics,
                reportContexts,
                report.Id);

            foreach (var snapshot in snapshots)
            {
                logs.Add(LongTermAnchorSeedItemLog.Create(
                    snapshot.SeedItemId,
                    report.Id,
                    report.ProductionOutput!.StartMonth,
                    report.ProductionOutput.EndMonth,
                    snapshot.PendingValueStartPeriod,
                    snapshot.IssuedQuantity,
                    snapshot.UnitPrice,
                    snapshot.TotalAmount,
                    snapshot.OriginAmount,
                    snapshot.TotalValueToAccount,
                    snapshot.UsageTime,
                    snapshot.AllocatedTime,
                    snapshot.RemainingTime,
                    snapshot.ActualOutput,
                    snapshot.PlannedOutput,
                    snapshot.StandardOutput,
                    snapshot.ValueByStandard,
                    snapshot.AllocationRatio,
                    snapshot.AccountedValueThisPeriod,
                    snapshot.PendingValueEndPeriod,
                    snapshot.Note));
            }
        }

        return logs;
    }

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
}
