using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.CuttingThickness.Queries;

public record GetCuttingThicknessByIdQuery(DefaultIdType Id) : IRequest<CuttingThicknessDto>;

public class GetCuttingThicknessByIdHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCuttingThicknessByIdQuery, CuttingThicknessDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<Domain.Entities.Index.CuttingThickness>();

    public async Task<CuttingThicknessDto> Handle(GetCuttingThicknessByIdQuery request, CancellationToken cancellationToken)
    {
        var cuttingThickness = await _cuttingThicknessRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return cuttingThickness.Adapt<CuttingThicknessDto>();
    }
}
