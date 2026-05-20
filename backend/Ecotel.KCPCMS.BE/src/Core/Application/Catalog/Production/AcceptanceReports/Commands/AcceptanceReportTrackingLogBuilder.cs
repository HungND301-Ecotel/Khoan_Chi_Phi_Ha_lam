using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

internal static class AcceptanceReportTrackingLogBuilder
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

    internal static List<AcceptanceReportItemLog> BuildTrackingLogs(
        Guid acceptanceReportId,
        IEnumerable<AcceptanceReportItem> items,
        IList<Part> allParts,
        ProductionOutput productionOutput,
        IReadOnlyDictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> outputByProcessGroup)
    {
        var logsToCreate = new List<AcceptanceReportItemLog>();

        foreach (var item in items)
        {
            var residualQuantity = item.IssuedQuantity - item.ShippedQuantity;
            if (!ShouldCreateLongTermTracking(item, residualQuantity))
            {
                continue;
            }

            var part = ResolveTrackedMaterial(item, allParts);
            if (part == null)
            {
                continue;
            }

            var cost = part.Costs?.FirstOrDefault(c =>
                c.StartMonth <= productionOutput.StartMonth &&
                c.EndMonth >= productionOutput.EndMonth);

            var unitPrice = (decimal)(cost?.Amount ?? 0);

            foreach (var trackingAllocation in BuildTrackingAllocations(item, residualQuantity))
            {
                var actualOutput = productionOutput.ProductionMeters;
                var plannedOutput = 1.0;
                var standardOutput = productionOutput.StandardProductionMeters;

                if (trackingAllocation.ProcessGroupId.HasValue &&
                    outputByProcessGroup.TryGetValue(trackingAllocation.ProcessGroupId.Value, out var metrics))
                {
                    actualOutput = metrics.ActualOutput;
                    plannedOutput = metrics.PlannedOutput;
                    standardOutput = metrics.StandardOutput;
                }

                logsToCreate.Add(AcceptanceReportItemLog.Create(
                    acceptanceReportItemId: item.Id,
                    acceptanceReportId: acceptanceReportId,
                    periodStartMonth: productionOutput.StartMonth,
                    periodEndMonth: productionOutput.EndMonth,
                    pendingValueStartPeriod: 0,
                    issuedQuantity: trackingAllocation.Quantity,
                    unitPrice: unitPrice,
                    usageTime: item.UsageTime,
                    allocatedTime: 0,
                    actualOutput: actualOutput,
                    plannedOutput: plannedOutput,
                    standardOutput: standardOutput,
                    allocationRatio: 1.0,
                    acceptanceReportItemCategoryAllocationId: trackingAllocation.CategoryAllocationId));
            }
        }

        return logsToCreate;
    }

    private static IList<(Guid? CategoryAllocationId, Guid? ProcessGroupId, double Quantity)> BuildTrackingAllocations(
        AcceptanceReportItem item,
        double residualQuantity)
    {
        if (residualQuantity <= 0)
        {
            return [];
        }

        if (item.CategoryAllocations.Any())
        {
            var totalAllocationQuantity = item.CategoryAllocations.Sum(x => x.Quantity);
            if (totalAllocationQuantity <= 0)
            {
                return
                [
                    (
                        CategoryAllocationId: (Guid?)null,
                        ProcessGroupId: item.ProcessGroupId,
                        Quantity: residualQuantity
                    )
                ];
            }

            return item.CategoryAllocations
                .Select(allocation => (
                    CategoryAllocationId: (Guid?)allocation.Id,
                    ProcessGroupId: (Guid?)allocation.ProcessGroupId,
                    Quantity: residualQuantity * allocation.Quantity / totalAllocationQuantity))
                .Where(x => x.Quantity > 0)
                .ToList();
        }

        return
        [
            (
                CategoryAllocationId: (Guid?)null,
                ProcessGroupId: item.ProcessGroupId,
                Quantity: residualQuantity
            )
        ];
    }

    private static bool ShouldCreateLongTermTracking(AcceptanceReportItem item, double residualQuantity)
        => item.IsTrackedSctxItem
            && item.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
            && item.IsLongTermTracking
            && residualQuantity > 0;

    private static Part? ResolveTrackedMaterial(AcceptanceReportItem item, IList<Part> allParts)
        => item.TrackedMaterialId.HasValue
            ? allParts.FirstOrDefault(p => p.Id == item.TrackedMaterialId.Value)
            : null;
}
