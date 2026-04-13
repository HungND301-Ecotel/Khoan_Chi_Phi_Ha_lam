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

        return await _monthCalculationService.CalculateAsync(
            month,
            year,
            hasProcessGroupFilter ? processGroupId : null,
            hasDepartmentFilter ? departmentId : null,
            cancellationToken);
    }
}
