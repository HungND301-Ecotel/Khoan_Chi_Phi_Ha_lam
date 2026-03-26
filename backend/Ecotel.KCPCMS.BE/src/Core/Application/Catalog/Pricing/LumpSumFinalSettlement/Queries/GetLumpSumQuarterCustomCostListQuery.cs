using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LumpSumFinalSettlement;
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

        var items = await customCostRepository.GetAllAsync(
            predicate: x => x.Quarter == quarter
                && x.Year == year
                && (!hasProcessGroup || x.ProcessGroupId == processGroupId),
            disableTracking: true);

        return items
            .OrderBy(x => x.CreatedOn)
            .Select(x => new LumpSumQuarterCustomCostDto
            {
                Id = x.Id,
                Quarter = x.Quarter,
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
}
