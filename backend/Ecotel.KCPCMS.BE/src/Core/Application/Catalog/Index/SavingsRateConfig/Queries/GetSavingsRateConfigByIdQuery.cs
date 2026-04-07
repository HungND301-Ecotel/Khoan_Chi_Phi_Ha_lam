using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SavingsRateConfig;
using Mapster;
using MediatR;
using Shared.Constants;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Queries;

public record GetSavingsRateConfigByIdQuery(DefaultIdType Id) : IRequest<SavingsRateConfigDto>;

public class GetSavingsRateConfigByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetSavingsRateConfigByIdQuery, SavingsRateConfigDto>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<SavingsRateConfigDto> Handle(GetSavingsRateConfigByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _savingsRateConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return entity.Adapt<SavingsRateConfigDto>();
    }
}
