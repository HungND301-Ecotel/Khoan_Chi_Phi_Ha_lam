using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.UnitOfMeasures.Queries;
public record GetUnitOfMeasureByIdQuery(DefaultIdType Id) : IRequest<UnitOfMeasureDto>;

public class GetUnitOfMeasureByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUnitOfMeasureByIdQuery, UnitOfMeasureDto>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<UnitOfMeasureDto> Handle(GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
    {
        var unitOfMeasure = await _unitOfMeasureRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return unitOfMeasure.Adapt<UnitOfMeasureDto>();
    }
}
