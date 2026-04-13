using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using Application.Catalog.Pricing.LumpSumFinalSettlement;
using Domain.Entities.Production;
using MediatR;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record GetLumpSumQuarterCustomCostListQuery(string Quarter, string Year, string ProcessGroupId) : IRequest<List<LumpSumQuarterCustomCostDto>>;

public class GetLumpSumQuarterCustomCostListQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLumpSumQuarterCustomCostListQuery, List<LumpSumQuarterCustomCostDto>>
{
    private readonly IWriteRepository<LumpSumQuarterCustomCost> customCostRepository = unitOfWork.GetRepository<LumpSumQuarterCustomCost>();
    public async Task<List<LumpSumQuarterCustomCostDto>> Handle(GetLumpSumQuarterCustomCostListQuery request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Quarter, out var quarter) || quarter < 1 || quarter > 4)
        {
            throw new BadRequestException("Invalid quarter");
        }

        if (!int.TryParse(request.Year, out var year))
        {
            throw new BadRequestException("Invalid year");
        }

        var hasProcessGroup = Guid.TryParse(request.ProcessGroupId, out var processGroupId);

        var monthList = GetMonthListByQuarter(quarter);
        var items = await customCostRepository.GetAllAsync(
            predicate: x => monthList.Contains(x.Month)
                && x.Year == year
                && (!hasProcessGroup || x.ProcessGroupId == processGroupId)
                && x.CustomName != LumpSumFinalSettlementSpecialQuantityKeys.CoalExcavation
                && x.CustomName != LumpSumFinalSettlementSpecialQuantityKeys.CoalCrosscut,
            disableTracking: true);

        return items
            .OrderBy(x => x.CreatedOn)
            .Select(x => new LumpSumQuarterCustomCostDto
            {
                Id = x.Id,
                Month = x.Month,
                Year = x.Year,
                ProcessGroupId = x.ProcessGroupId,
                CustomName = x.CustomName,
                ActualQuantity = x.ActualQuantity,
                MaterialUnitPrice = x.MaterialUnitPrice,
                MaintainUnitPrice = x.MaintainUnitPrice,
                ElectricityUnitPrice = x.ElectricityUnitPrice
            })
            .ToList();
    }

    public static List<int> GetMonthListByQuarter(int quarter)
    {
        switch (quarter)
        {
            case 1:
                return [1, 2, 3];
            case 2:
                return [4, 5, 6];
            case 3:
                return [7, 8, 9];
            case 4:
                return [10, 11, 12];
            default:
                throw new BadRequestException("Invalid quarter or year");
                break;
        }
    }
}
