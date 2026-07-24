using Application.Common.Exceptions;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using MediatR;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record GetLumpSumFinalSettlementListQuery(string Month, string Year, string? ProcessGroupId, string? DepartmentId) : IRequest<LumpSumFinalSettlementMonthResponseDto>;

public class GetLumpSumFinalSettlementListQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLumpSumFinalSettlementListQuery, LumpSumFinalSettlementMonthResponseDto>
{
    private readonly LumpSumFinalSettlementMonthCalculationService _monthCalculationService = new(unitOfWork);

    public async Task<LumpSumFinalSettlementMonthResponseDto> Handle(GetLumpSumFinalSettlementListQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Month, out var month) || !int.TryParse(request.Year, out var year))
        {
            throw new BadRequestException("Invalid month or year");
        }

        if (month < 1 || month > 12)
        {
            throw new BadRequestException("Month must be from 1 to 12");
        }

        var hasProcessGroupFilter = Guid.TryParse(request.ProcessGroupId, out var processGroupId);
        var hasDepartmentFilter = Guid.TryParse(request.DepartmentId, out var departmentId);

        var result = await _monthCalculationService.CalculateAsync(
            month,
            year,
            hasProcessGroupFilter ? processGroupId : null,
            hasDepartmentFilter ? departmentId : null,
            cancellationToken);
        if (month % 3 == 0)
        {
            var quarter = (month - 1) / 3 + 1;
            var monthList = GetLumpSumFinalSettlementQuarterListQueryHandler.GetMonthListByQuarter(quarter);

            var monthBreakdowns = new List<LumpSumFinalSettlementMonthResponseDto>();
            foreach (var m in monthList)
            {
                var monthResult = m == month
                    ? result
                    : await _monthCalculationService.CalculateAsync(
                        m,
                        year,
                        hasProcessGroupFilter ? processGroupId : null,
                        hasDepartmentFilter ? departmentId : null,
                        cancellationToken);
                monthBreakdowns.Add(monthResult);
            }

            var quarterBreakdown = LumpSumFinalSettlementQuarterAggregator.Build(monthBreakdowns);

            var accepted = await _monthCalculationService.CalculateQuarterAcceptedSavingAsync(
                quarterBreakdown.RevenueQuarter.TotalAmount,
                cancellationToken);

            quarterBreakdown.SavingsValue = accepted.SavingsValue;
            quarterBreakdown.QuyetToanSavingsLimitQuarter = accepted.QuyetToanSavingsLimitQuarter;
            quarterBreakdown.AcceptedSavingQuarter = accepted.AcceptedSavingQuarter;
            quarterBreakdown.RevenueAdjustmentRate = accepted.RevenueAdjustmentRate;
            quarterBreakdown.SavingAddedToIncomeQuarter = accepted.SavingAddedToIncomeQuarter;

            result.QuarterBreakdown = quarterBreakdown;
        }

        return result;


    }
}
