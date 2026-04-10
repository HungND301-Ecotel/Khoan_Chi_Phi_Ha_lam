using Domain.Entities.Pricing.MaterialUnitPrice;

namespace Application.Catalog.Pricing.Common;

public static class PlannedMaterialCostCalculator
{
    public static Dictionary<Guid, double> CalculateUnitPricesByCostId(
        IReadOnlyCollection<Domain.Entities.Pricing.PlannedMaterialCost> plannedMaterialCosts,
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPrices)
    {
        var result = new Dictionary<Guid, double>();

        foreach (var cost in plannedMaterialCosts)
        {
            result[cost.Id] = CalculateUnitPrice(cost, tunnelMaterialUnitPrices);
        }

        return result;
    }

    private static double CalculateUnitPrice(
        Domain.Entities.Pricing.PlannedMaterialCost plannedMaterialCost,
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterialUnitPrices)
    {
        var currentMaterialUnitPrice = plannedMaterialCost.MaterialUnitPrice;
        if (currentMaterialUnitPrice == null)
        {
            return 0;
        }

        var effectiveMonth = plannedMaterialCost.Output?.StartMonth ?? DateOnly.MinValue;
        var slideCost = plannedMaterialCost.SlideUnitPriceAssignmentCode?.Amount ?? 0;
        var currentAssignmentTotals = currentMaterialUnitPrice.MaterialUnitPriceAssignmentCodes
            .GroupBy(a => a.AssignmentCodeId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));

        if (plannedMaterialCost.NormFactor == null)
        {
            return slideCost + ApplyOtherMaterialValue(currentAssignmentTotals.Values.Sum(), currentMaterialUnitPrice.OtherMaterialvalue);
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

        return slideCost + totalMaterialAssignments;
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
        return total + otherMaterialValue;
    }
}
