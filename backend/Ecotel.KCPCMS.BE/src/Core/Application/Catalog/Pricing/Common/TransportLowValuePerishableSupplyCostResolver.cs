using Application.Common.Repositories;
using Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.Common;

public static class TransportLowValuePerishableSupplyCostResolver
{
    public readonly record struct Key(Guid DepartmentId, Guid ProcessGroupId, DateOnly Month);

    public static async Task<Dictionary<Key, double>> ResolveAsync(
        IReadOnlyCollection<TransportPlanLineEntity> lines,
        IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPriceRepository,
        CancellationToken cancellationToken)
    {
        var includedKeys = lines
            .Where(x => x.PlannedTransportCost?.LowValuePerishableSupplyInclusion
                    == LowValuePerishableSupplyInclusion.Include
                && x.ProductionProcess?.ProcessGroupId != null)
            .Select(x => new Key(x.DepartmentId, x.ProductionProcess!.ProcessGroupId, x.StartMonth))
            .Distinct()
            .ToList();

        if (includedKeys.Count == 0)
        {
            return [];
        }

        var departmentIds = includedKeys.Select(x => x.DepartmentId).Distinct().ToList();
        var processGroupIds = includedKeys.Select(x => x.ProcessGroupId).Distinct().ToList();
        var minMonth = includedKeys.Min(x => x.Month);
        var maxMonth = includedKeys.Max(x => x.Month);

        var catalog = await lowValuePerishableSupplyUnitPriceRepository.GetAll()
            .Where(x => x.Type == LowValuePerishableSupplyType.Transport
                && departmentIds.Contains(x.DepartmentId)
                && processGroupIds.Contains(x.ProcessGroupId)
                && x.StartMonth <= maxMonth
                && x.EndMonth >= minMonth)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Key, double>();
        foreach (var key in includedKeys)
        {
            var price = catalog
                .Where(x => x.DepartmentId == key.DepartmentId
                    && x.ProcessGroupId == key.ProcessGroupId
                    && x.StartMonth <= key.Month
                    && x.EndMonth >= key.Month)
                .OrderByDescending(x => x.StartMonth)
                .ThenByDescending(x => x.EndMonth)
                .Select(x => x.TotalPrice)
                .FirstOrDefault();

            result[key] = price;
        }

        return result;
    }

    public static double Resolve(TransportPlanLineEntity line, Dictionary<Key, double> lookup)
    {
        if (line.PlannedTransportCost?.LowValuePerishableSupplyInclusion != LowValuePerishableSupplyInclusion.Include
            || line.ProductionProcess?.ProcessGroupId == null)
        {
            return 0;
        }

        var key = new Key(line.DepartmentId, line.ProductionProcess.ProcessGroupId, line.StartMonth);
        return lookup.TryGetValue(key, out var price) ? price : 0;
    }
}
