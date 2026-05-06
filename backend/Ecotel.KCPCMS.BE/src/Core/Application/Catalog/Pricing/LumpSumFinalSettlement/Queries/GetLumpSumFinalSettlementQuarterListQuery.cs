using Application.Common.Exceptions;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using MediatR;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record GetLumpSumFinalSettlementQuarterListQuery(string Quarter, string Year, string? ProcessGroupId, string? DepartmentId) : IRequest<LumpSumFinalSettlementQuarterResponseDto>;

public class GetLumpSumFinalSettlementQuarterListQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLumpSumFinalSettlementQuarterListQuery, LumpSumFinalSettlementQuarterResponseDto>
{
    private readonly LumpSumFinalSettlementMonthCalculationService _monthCalculationService = new(unitOfWork);

    public async Task<LumpSumFinalSettlementQuarterResponseDto> Handle(GetLumpSumFinalSettlementQuarterListQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Quarter, out var quarter) || !int.TryParse(request.Year, out var year))
        {
            throw new BadRequestException("Invalid quarter or year");
        }

        if (quarter < 1 || quarter > 4)
        {
            throw new BadRequestException("Quarter must be from 1 to 4");
        }

        var hasProcessGroupFilter = Guid.TryParse(request.ProcessGroupId, out var processGroupId);
        var hasDepartmentFilter = Guid.TryParse(request.DepartmentId, out var departmentId);

        var monthList = GetMonthListByQuarter(quarter);
        var monthBreakdowns = new List<LumpSumFinalSettlementMonthResponseDto>();
        foreach (var month in monthList)
        {
            var monthResult = await _monthCalculationService.CalculateAsync(
                month,
                year,
                hasProcessGroupFilter ? processGroupId : null,
                hasDepartmentFilter ? departmentId : null,
                cancellationToken);
            monthBreakdowns.Add(monthResult);
        }

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
                var materialTotal = group.Sum(x => x.Materials?.TotalAmount ?? 0);
                var maintainTotal = group.Sum(x => x.Maintains?.TotalAmount ?? 0);
                var electricityTotal = group.Sum(x => x.Electricities?.TotalAmount ?? 0);

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

    public static List<int> GetMonthListByQuarter(int quarter)
    {
        return quarter switch
        {
            1 => [1, 2, 3],
            2 => [4, 5, 6],
            3 => [7, 8, 9],
            4 => [10, 11, 12],
            _ => throw new BadRequestException("Invalid quarter or year")
        };
    }
}
