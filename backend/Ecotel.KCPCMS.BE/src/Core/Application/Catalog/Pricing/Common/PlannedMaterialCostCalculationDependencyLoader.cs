using Application.Common.Repositories;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.Common;

public sealed class PlannedMaterialCostCalculationDependencies
{
    public IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> TunnelMaterialUnitPrices { get; init; } = [];
    public IReadOnlyCollection<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> LowValuePerishableSupplyUnitPrices { get; init; } = [];
}

public static class PlannedMaterialCostCalculationDependencyLoader
{
    public static async Task<PlannedMaterialCostCalculationDependencies> LoadAsync(
        IReadOnlyCollection<Domain.Entities.Pricing.PlannedMaterialCost> plannedMaterialCosts,
        IWriteRepository<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPriceRepository,
        IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPriceRepository,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = [];
        var currentTunnelMaterials = plannedMaterialCosts
            .Where(c => c.NormFactor != null
                && c.NormFactor.NormFactorAssignmentCodes.Any(nfa => nfa.TargetHardnessId.HasValue)
                && c.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice)
            .Select(c => (TunnelExcavationMaterialUnitPrice)c.MaterialUnitPrice!)
            .ToList();

        if (currentTunnelMaterials.Any())
        {
            var targetHardnessIds = plannedMaterialCosts
                .Where(c => c.NormFactor != null)
                .SelectMany(c => c.NormFactor!.NormFactorAssignmentCodes
                    .Where(nfa => nfa.TargetHardnessId.HasValue)
                    .Select(nfa => nfa.TargetHardnessId!.Value))
                .Distinct()
                .ToList();
            var processIds = currentTunnelMaterials.Select(x => x.ProcessId).Distinct().ToList();
            var passportIds = currentTunnelMaterials.Select(x => x.PassportId).Distinct().ToList();
            var insertItemIds = currentTunnelMaterials.Select(x => x.InsertItemId).Distinct().ToList();
            var supportStepIds = currentTunnelMaterials.Select(x => x.SupportStepId).Distinct().ToList();

            tunnelMaterials = await tunnelMaterialUnitPriceRepository.GetAll()
                .Where(x => targetHardnessIds.Contains(x.HardnessId)
                    && processIds.Contains(x.ProcessId)
                    && passportIds.Contains(x.PassportId)
                    && insertItemIds.Contains(x.InsertItemId)
                    && supportStepIds.Contains(x.SupportStepId))
                .Include(x => x.MaterialUnitPriceAssignmentCodes)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        IReadOnlyCollection<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValueUnitPrices = [];
        var includedLowValueCosts = plannedMaterialCosts
            .Where(c => c.LowValuePerishableSupplyInclusion == Domain.Common.Enums.LowValuePerishableSupplyInclusion.Include
                && c.ProductUnitPrice?.DepartmentId != null
                && c.ProductUnitPrice.Product != null
                && c.Output != null)
            .ToList();

        if (includedLowValueCosts.Any())
        {
            var departmentIds = includedLowValueCosts
                .Select(c => c.ProductUnitPrice!.DepartmentId!.Value)
                .Distinct()
                .ToList();
            var processGroupIds = includedLowValueCosts
                .Select(c => c.ProductUnitPrice!.Product!.ProcessGroupId)
                .Distinct()
                .ToList();
            var minMonth = includedLowValueCosts.Min(c => c.Output!.StartMonth);
            var maxMonth = includedLowValueCosts.Max(c => c.Output!.StartMonth);

            lowValueUnitPrices = await lowValuePerishableSupplyUnitPriceRepository.GetAll()
                .Where(x => departmentIds.Contains(x.DepartmentId)
                    && processGroupIds.Contains(x.ProcessGroupId)
                    && x.StartMonth <= maxMonth
                    && x.EndMonth >= minMonth)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        return new PlannedMaterialCostCalculationDependencies
        {
            TunnelMaterialUnitPrices = tunnelMaterials,
            LowValuePerishableSupplyUnitPrices = lowValueUnitPrices,
        };
    }
}