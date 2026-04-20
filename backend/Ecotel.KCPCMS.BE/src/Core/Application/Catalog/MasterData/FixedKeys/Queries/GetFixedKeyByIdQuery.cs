using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MasterData;
using Domain.Entities.MasterData;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.MasterData.FixedKeys.Queries;

public record GetFixedKeyByIdQuery(Guid Id) : IRequest<FixedKeyDto>;

public class GetFixedKeyByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetFixedKeyByIdQuery, FixedKeyDto>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<FixedKeyDto> Handle(GetFixedKeyByIdQuery request, CancellationToken cancellationToken)
    {
        var fixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return fixedKey.Adapt<FixedKeyDto>();
    }
}