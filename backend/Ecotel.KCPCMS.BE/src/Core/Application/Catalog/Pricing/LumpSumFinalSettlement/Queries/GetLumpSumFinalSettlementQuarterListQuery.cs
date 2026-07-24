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

        var result = LumpSumFinalSettlementQuarterAggregator.Build(monthBreakdowns);

        var accepted = await _monthCalculationService.CalculateQuarterAcceptedSavingAsync(
            result.RevenueQuarter.TotalAmount,
            cancellationToken);

        result.SavingsValue = accepted.SavingsValue;
        result.QuyetToanSavingsLimitQuarter = accepted.QuyetToanSavingsLimitQuarter;
        result.AcceptedSavingQuarter = accepted.AcceptedSavingQuarter;
        result.RevenueAdjustmentRate = accepted.RevenueAdjustmentRate;
        result.SavingAddedToIncomeQuarter = accepted.SavingAddedToIncomeQuarter;

        return result;

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