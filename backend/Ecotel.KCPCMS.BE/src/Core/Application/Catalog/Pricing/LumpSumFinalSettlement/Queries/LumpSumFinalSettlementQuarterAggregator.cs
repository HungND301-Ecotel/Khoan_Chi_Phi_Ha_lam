using Application.Dto.Catalog.LumpSumFinalSettlement;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public static class LumpSumFinalSettlementQuarterAggregator
{
    public static LumpSumFinalSettlementQuarterResponseDto Build(List<LumpSumFinalSettlementMonthResponseDto> monthBreakdowns)
    {
        var allItems = monthBreakdowns.SelectMany(x => x.Items).ToList();
        var aggregatedItems = AggregateByProduct(allItems);

        var revenuesByMonth = monthBreakdowns
            .Select(x => x.Revenue)
            .OrderBy(x => x.Month)
            .ToList();

        var costsByMonth = monthBreakdowns
            .Select(x => x.Cost)
            .OrderBy(x => x.Month)
            .ToList();

        var savingsByMonth = monthBreakdowns
            .Select(x => x.Saving)
            .OrderBy(x => x.Month)
            .ToList();

        var transferredCosts = monthBreakdowns
            .Select(x => x.TransferredCost)
            .OrderBy(x => x.Month)
            .ToList();

        var quarterCustomCosts = monthBreakdowns
            .SelectMany(x => x.CustomCosts)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        var coalExcavationActualQuantity = monthBreakdowns.Sum(x => x.CoalExcavationActualQuantity);
        var coalCrosscutActualQuantity = monthBreakdowns.Sum(x => x.CoalCrosscutActualQuantity);
        var meterExcavationActualQuantity = monthBreakdowns.Sum(x => x.MeterExcavationActualQuantity);
        var meterCrosscutActualQuantity = monthBreakdowns.Sum(x => x.MeterCrosscutActualQuantity);

        var revenueQuarter = SumMonthDetails(revenuesByMonth);
        var costQuarter = SumMonthDetails(costsByMonth);
        var savingQuarter = SumMonthDetails(savingsByMonth);
        var totalSavingQuarter = monthBreakdowns.Sum(x => x.TotalSavingMonth);
        var quyetToanSavingsLimitQuarter = monthBreakdowns.Sum(x => x.QuyetToanSavingsLimit);
        var acceptedSavingQuarter = monthBreakdowns.Sum(x => x.AcceptedSavingMonth);
        var savingAddedToIncomeQuarter = monthBreakdowns.Sum(x => x.SavingAddedToIncomeMonth);
        var savingsValue = Math.Abs(totalSavingQuarter) > double.Epsilon
            ? acceptedSavingQuarter / totalSavingQuarter
            : 0;
        var revenueAdjustmentRate = Math.Abs(acceptedSavingQuarter) > double.Epsilon
            ? savingAddedToIncomeQuarter / acceptedSavingQuarter
            : 0;

        return new LumpSumFinalSettlementQuarterResponseDto
        {
            Items = aggregatedItems,
            MonthBreakdowns = monthBreakdowns.OrderBy(x => x.Revenue.Month).ToList(),
            RevenuesByMonth = revenuesByMonth,
            CostsByMonth = costsByMonth,
            SavingsByMonth = savingsByMonth,
            TransferredCosts = transferredCosts,
            CustomCosts = quarterCustomCosts,
            RevenueQuarter = revenueQuarter,
            CostQuarter = costQuarter,
            SavingQuarter = savingQuarter,
            CoalExcavationActualQuantity = coalExcavationActualQuantity,
            CoalCrosscutActualQuantity = coalCrosscutActualQuantity,
            MeterExcavationActualQuantity = meterExcavationActualQuantity,
            MeterCrosscutActualQuantity = meterCrosscutActualQuantity,
            TotalSavingQuarter = totalSavingQuarter,
            QuyetToanSavingsLimitQuarter = quyetToanSavingsLimitQuarter,
            AcceptedSavingQuarter = acceptedSavingQuarter,
            SavingsValue = savingsValue,
            RevenueAdjustmentRate = revenueAdjustmentRate,
            SavingAddedToIncomeQuarter = savingAddedToIncomeQuarter
        };
    }

    private static LumpSumQuarterRevenueByMonthDto SumMonthDetails(IEnumerable<LumpSumQuarterRevenueByMonthDto> details)
    {
        var list = details.ToList();
        return new LumpSumQuarterRevenueByMonthDto
        {
            Materials = new LumpSumCostDetailDto { TotalAmount = list.Sum(x => x.Materials.TotalAmount) },
            Maintains = new LumpSumCostDetailDto { TotalAmount = list.Sum(x => x.Maintains.TotalAmount) },
            Electricities = new LumpSumCostDetailDto { TotalAmount = list.Sum(x => x.Electricities.TotalAmount) },
            TotalAmount = list.Sum(x => x.TotalAmount)
        };
    }

    private static List<LumpSumFinalSettlementDto> AggregateByProduct(IEnumerable<LumpSumFinalSettlementDto> items)
    {
        return items
            .GroupBy(x => new
            {
                x.ProcessGroupId,
                x.ProcessGroupCode,
                x.ProcessGroupName,
                x.ProductCode,
                x.ProductName,
                x.UnitOfMeasureId,
                x.UnitOfMeasureName
            })
            .Select(group =>
            {
                var plannedQuantity = group.Sum(x => x.PlannedQuantity);
                var actualQuantity = group.Sum(x => x.ActualQuantity);
                var materialTotal = group.Sum(x => (x.Materials?.TotalAmount ?? 0) + (x.AshContentMaterials?.TotalAmount ?? 0));
                var maintainTotal = group.Sum(x => (x.Maintains?.TotalAmount ?? 0) + (x.AshContentMaintains?.TotalAmount ?? 0));
                var electricityTotal = group.Sum(x => (x.Electricities?.TotalAmount ?? 0) + (x.AshContentElectricities?.TotalAmount ?? 0));

                return new LumpSumFinalSettlementDto
                {
                    Id = group.Select(x => x.Id).FirstOrDefault(),
                    ProcessGroupId = group.Key.ProcessGroupId,
                    ProcessGroupCode = group.Key.ProcessGroupCode,
                    ProcessGroupName = group.Key.ProcessGroupName,
                    ProductCode = group.Key.ProductCode,
                    ProductName = group.Key.ProductName,
                    UnitOfMeasureId = group.Key.UnitOfMeasureId,
                    UnitOfMeasureName = group.Key.UnitOfMeasureName,
                    PlannedQuantity = plannedQuantity,
                    ActualQuantity = actualQuantity,
                    Materials = new LumpSumCostDetailDto
                    {
                        UnitPrice = actualQuantity > 0 ? materialTotal / actualQuantity : 0,
                        TotalAmount = materialTotal
                    },
                    Maintains = new LumpSumCostDetailDto
                    {
                        UnitPrice = actualQuantity > 0 ? maintainTotal / actualQuantity : 0,
                        TotalAmount = maintainTotal
                    },
                    Electricities = new LumpSumCostDetailDto
                    {
                        UnitPrice = actualQuantity > 0 ? electricityTotal / actualQuantity : 0,
                        TotalAmount = electricityTotal
                    },
                    TotalAmount = materialTotal + maintainTotal + electricityTotal
                };
            })
            .OrderBy(x => x.ProcessGroupCode)
            .ThenBy(x => x.ProcessGroupName)
            .ThenBy(x => x.ProductCode)
            .ThenBy(x => x.ProductName)
            .ToList();
    }
}