using Domain.Entities.Pricing.MaterialUnitPrice;

namespace Application.Catalog.Pricing.Common;

public sealed class PlannedMaterialCostCalculationResult
{
    public double TotalPrice { get; init; }
    public double LowValuePerishableSupplyUnitPriceCost { get; init; }
}

public static class PlannedMaterialCostCalculator
{
    public static Dictionary<Guid, double> CalculateUnitPricesByCostId(
        IReadOnlyCollection<Domain.Entities.Pricing.PlannedMaterialCost> plannedMaterialCosts,
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPrices,
        IReadOnlyCollection<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPrices)
    {
        return CalculateResultsByCostId(plannedMaterialCosts, tunnelMaterialUnitPrices, lowValuePerishableSupplyUnitPrices)
            .ToDictionary(x => x.Key, x => x.Value.TotalPrice);
    }

    public static Dictionary<Guid, PlannedMaterialCostCalculationResult> CalculateResultsByCostId(
        IReadOnlyCollection<Domain.Entities.Pricing.PlannedMaterialCost> plannedMaterialCosts,
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPrices,
        IReadOnlyCollection<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPrices)
    {
        var result = new Dictionary<Guid, PlannedMaterialCostCalculationResult>();

        foreach (var cost in plannedMaterialCosts)
        {
            result[cost.Id] = CalculateUnitPrice(cost, tunnelMaterialUnitPrices, lowValuePerishableSupplyUnitPrices);
        }

        return result;
    }

    private static PlannedMaterialCostCalculationResult CalculateUnitPrice(
        Domain.Entities.Pricing.PlannedMaterialCost plannedMaterialCost,
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPrices,
        IReadOnlyCollection<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPrices)
    {
        var currentMaterialUnitPrice = plannedMaterialCost.MaterialUnitPrice;
        if (currentMaterialUnitPrice == null)
        {
            return new PlannedMaterialCostCalculationResult();
        }

        var effectiveMonth = plannedMaterialCost.Output?.StartMonth ?? DateOnly.MinValue;
        var slideCost = plannedMaterialCost.SlideUnitPriceAssignmentCode?.Amount ?? 0;
        var lowValueCost = ResolveLowValuePerishableSupplyCost(plannedMaterialCost, effectiveMonth, lowValuePerishableSupplyUnitPrices);
        var currentAssignmentTotals = currentMaterialUnitPrice.MaterialUnitPriceAssignmentCodes
            .GroupBy(a => a.AssignmentCodeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));

        if (plannedMaterialCost.NormFactor == null)
        {
            var total = slideCost + ApplyOtherMaterialValue(currentAssignmentTotals.Values.Sum(), currentMaterialUnitPrice.OtherMaterialvalue) + lowValueCost;
            return new PlannedMaterialCostCalculationResult
            {
                TotalPrice = total,
                LowValuePerishableSupplyUnitPriceCost = lowValueCost,
            };
        }

        var normFactor = plannedMaterialCost.NormFactor;
        var affectedAssignments = normFactor.NormFactorAssignmentCodes.ToList();
        var affectedAssignmentCodeIds = affectedAssignments.Select(x => x.AssignmentCodeId).ToHashSet();

        var unaffectedTotal = currentAssignmentTotals
            .Where(x => !affectedAssignmentCodeIds.Contains(x.Key))
            .Sum(x => x.Value);

        var affectedTotal = 0d;
        foreach (var affectedAssignment in affectedAssignments)
        {
            var assignmentCodeId = affectedAssignment.AssignmentCodeId;
            var assignmentAmount = currentAssignmentTotals.GetValueOrDefault(assignmentCodeId, 0);

            if (affectedAssignment.TargetHardnessId.HasValue &&
                currentMaterialUnitPrice is TunnelExcavationMaterialUnitPrice currentTunnelMaterialUnitPrice)
            {
                var targetMaterialUnitPrice = ResolveTargetTunnelMaterialUnitPrice(
                    currentTunnelMaterialUnitPrice,
                    affectedAssignment.TargetHardnessId.Value,
                    effectiveMonth,
                    tunnelMaterialUnitPrices);

                var targetAssignmentTotals = targetMaterialUnitPrice?.MaterialUnitPriceAssignmentCodes
                    .GroupBy(a => a.AssignmentCodeId)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice))
                    ?? new Dictionary<Guid, double>();

                assignmentAmount = targetAssignmentTotals.GetValueOrDefault(assignmentCodeId, assignmentAmount);
            }

            affectedTotal += assignmentAmount * affectedAssignment.Value;
        }

        if (affectedAssignments.Count > 0)
        {
            affectedTotal = ApplyOtherMaterialValue(affectedTotal, currentMaterialUnitPrice.OtherMaterialvalue);
        }

        var totalMaterialAssignments = unaffectedTotal + affectedTotal;

        return new PlannedMaterialCostCalculationResult
        {
            TotalPrice = slideCost + totalMaterialAssignments + lowValueCost,
            LowValuePerishableSupplyUnitPriceCost = lowValueCost,
        };
    }

    private static double ResolveLowValuePerishableSupplyCost(
        Domain.Entities.Pricing.PlannedMaterialCost plannedMaterialCost,
        DateOnly effectiveMonth,
        IReadOnlyCollection<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> lowValuePerishableSupplyUnitPrices)
    {
        if (plannedMaterialCost.LowValuePerishableSupplyInclusion != Domain.Common.Enums.LowValuePerishableSupplyInclusion.Include)
        {
            return 0;
        }

        var departmentId = plannedMaterialCost.ProductUnitPrice?.DepartmentId;
        var processGroupId = plannedMaterialCost.ProductUnitPrice?.Product?.ProcessGroupId;
        if (!departmentId.HasValue || !processGroupId.HasValue)
        {
            return 0;
        }

        return lowValuePerishableSupplyUnitPrices
            .Where(x => x.DepartmentId == departmentId.Value
                && x.ProcessGroupId == processGroupId.Value
                && x.StartMonth <= effectiveMonth
                && x.EndMonth >= effectiveMonth)
            .OrderByDescending(x => x.StartMonth)
            .ThenByDescending(x => x.EndMonth)
            .Select(x => x.TotalPrice)
            .FirstOrDefault();
    }

    private static TunnelExcavationMaterialUnitPrice? ResolveTargetTunnelMaterialUnitPrice(
        TunnelExcavationMaterialUnitPrice currentMaterialUnitPrice,
        Guid targetHardnessId,
        DateOnly effectiveMonth,
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPrices)
    {
        return tunnelMaterialUnitPrices
            .Where(x =>
                x.ProcessId == currentMaterialUnitPrice.ProcessId &&
                x.PassportId == currentMaterialUnitPrice.PassportId &&
                x.InsertItemId == currentMaterialUnitPrice.InsertItemId &&
                x.SupportStepId == currentMaterialUnitPrice.SupportStepId &&
                x.HardnessId == targetHardnessId &&
                x.TechnologyId == currentMaterialUnitPrice.TechnologyId &&
                x.StartMonth <= effectiveMonth &&
                x.EndMonth >= effectiveMonth)
            .OrderByDescending(x => x.StartMonth)
            .ThenByDescending(x => x.EndMonth)
            .FirstOrDefault();
    }

    private static double ApplyOtherMaterialValue(double total, double otherMaterialValue)
    {
        return total * (1 + otherMaterialValue / 100.0);
    }
}
