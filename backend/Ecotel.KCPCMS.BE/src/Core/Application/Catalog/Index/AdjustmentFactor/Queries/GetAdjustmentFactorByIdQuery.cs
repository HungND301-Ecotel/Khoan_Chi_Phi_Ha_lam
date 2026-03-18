using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record GetAdjustmentFactorByIdQuery(DefaultIdType Id) : IRequest<AdjustmentFactorDto>;

public class GetAdjustmentFactorByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdjustmentFactorByIdQuery, AdjustmentFactorDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();

    public async Task<AdjustmentFactorDto> Handle(GetAdjustmentFactorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var adjustmentFactor = await _adjustmentFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return adjustmentFactor.Adapt<AdjustmentFactorDto>();
    }
}
