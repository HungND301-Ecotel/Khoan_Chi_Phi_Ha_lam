using Application.Common.Repositories;
using Domain.Common.Enums;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using Microsoft.EntityFrameworkCore;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.Common;

public static class TransportLowValuePerishableSupplyCostResolver
{
    public readonly record struct Key(Guid DepartmentId, Guid ProcessGroupId, DateOnly Month);

    // VTCG có 2 giá trị: VTMHRT (đ/tháng) + Điện năng (kWh, chỉ hiển thị — không quy ra tiền); VTL chỉ có VTMHRT
    public readonly record struct Price(double Amount, double? ElectricityKwh);

    public static async Task<Dictionary<Key, Price>> ResolveAsync(
        IReadOnlyCollection<TransportPlanLineEntity> lines,
        IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPriceRepository,
        IWriteRepository<MechanizedTransportOverheadUnitPrice> mechanizedTransportOverheadUnitPriceRepository,
        CancellationToken cancellationToken)
    {
        var includedLines = lines
            .Where(x => x.PlannedTransportCost?.LowValuePerishableSupplyInclusion
                    == LowValuePerishableSupplyInclusion.Include
                && x.ProductionProcess?.ProcessGroupId != null)
            .ToList();

        if (includedLines.Count == 0)
        {
            return [];
        }

        var includedKeys = includedLines
            .Select(x => new Key(x.DepartmentId, x.ProductionProcess!.ProcessGroupId, x.StartMonth))
            .Distinct()
            .ToList();

        var departmentIds = includedKeys.Select(x => x.DepartmentId).Distinct().ToList();
        var processGroupIds = includedKeys.Select(x => x.ProcessGroupId).Distinct().ToList();
        var minMonth = includedKeys.Min(x => x.Month);
        var maxMonth = includedKeys.Max(x => x.Month);

        // VTL catalog
        var vtlCatalog = await lowValuePerishableSupplyUnitPriceRepository.GetAll()
            .Where(x => x.Type == LowValuePerishableSupplyType.Transport
                && departmentIds.Contains(x.DepartmentId)
                && processGroupIds.Contains(x.ProcessGroupId)
                && x.StartMonth <= maxMonth
                && x.EndMonth >= minMonth)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // VTCG catalog
        var vtcgCatalog = await mechanizedTransportOverheadUnitPriceRepository.GetAll()
            .Where(x => departmentIds.Contains(x.DepartmentId)
                && processGroupIds.Contains(x.ProcessGroupId)
                && x.StartMonth <= maxMonth
                && x.EndMonth >= minMonth)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Process group classification lookup
        var vtcgProcessGroupIds = includedLines
            .Where(x => x.ProductionProcess?.ProcessGroup?.Type == ProcessGroupType.VTCG
                || x.ProductionProcess?.ProcessGroup?.FixedKey?.Key == "VTCG")
            .Select(x => x.ProductionProcess!.ProcessGroupId)
            .ToHashSet();

        var result = new Dictionary<Key, Price>();
        foreach (var key in includedKeys)
        {
            var isVtcg = vtcgProcessGroupIds.Contains(key.ProcessGroupId);
            if (isVtcg)
            {
                var matched = vtcgCatalog
                    .Where(x => x.DepartmentId == key.DepartmentId
                        && x.ProcessGroupId == key.ProcessGroupId
                        && x.StartMonth <= key.Month
                        && x.EndMonth >= key.Month)
                    .OrderByDescending(x => x.StartMonth)
                    .ThenByDescending(x => x.EndMonth)
                    .FirstOrDefault();

                result[key] = new Price(
                    (double?)matched?.LowValuePerishableSupplyUnitPrice ?? 0,
                    (double?)matched?.ElectricityUnitPrice);
            }
            else
            {
                var price = vtlCatalog
                    .Where(x => x.DepartmentId == key.DepartmentId
                        && x.ProcessGroupId == key.ProcessGroupId
                        && x.StartMonth <= key.Month
                        && x.EndMonth >= key.Month)
                    .OrderByDescending(x => x.StartMonth)
                    .ThenByDescending(x => x.EndMonth)
                    .Select(x => x.TotalPrice)
                    .FirstOrDefault();

                result[key] = new Price(price, null);
            }
        }

        return result;
    }

    public static double Resolve(TransportPlanLineEntity line, Dictionary<Key, Price> lookup) =>
        TryResolve(line, lookup)?.Amount ?? 0;

    public static double? ResolveElectricityKwh(TransportPlanLineEntity line, Dictionary<Key, Price> lookup) =>
        TryResolve(line, lookup)?.ElectricityKwh;

    private static Price? TryResolve(TransportPlanLineEntity line, Dictionary<Key, Price> lookup)
    {
        if (line.PlannedTransportCost?.LowValuePerishableSupplyInclusion != LowValuePerishableSupplyInclusion.Include
            || line.ProductionProcess?.ProcessGroupId == null)
        {
            return null;
        }

        var key = new Key(line.DepartmentId, line.ProductionProcess.ProcessGroupId, line.StartMonth);
        return lookup.TryGetValue(key, out var price) ? price : null;
    }
}
