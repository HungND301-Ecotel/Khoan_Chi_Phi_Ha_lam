using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using Mapster;
using MediatR;
using Shared.Constants;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Queries;

public record GetRevenueCostAdjustmentConfigByIdQuery(DefaultIdType Id) : IRequest<RevenueCostAdjustmentConfigDto>;

public class GetRevenueCostAdjustmentConfigByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRevenueCostAdjustmentConfigByIdQuery, RevenueCostAdjustmentConfigDto>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<RevenueCostAdjustmentConfigDto> Handle(GetRevenueCostAdjustmentConfigByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _revenueCostAdjustmentConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return entity.Adapt<RevenueCostAdjustmentConfigDto>();
    }
}
