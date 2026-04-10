using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Cost;
using Application.Dto.Catalog.Part;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Queries.Part;

public record GetOtherPartByIdQuery(DefaultIdType Id) : IRequest<OtherPartDetailDto>;

public class GetOtherPartByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetOtherPartByIdQuery, OtherPartDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    public async Task<OtherPartDetailDto> Handle(GetOtherPartByIdQuery request, CancellationToken cancellationToken)
    {
        var details = await _partRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(t => t.UnitOfMeasure)
                .Include(t => t.Costs)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new OtherPartDetailDto
        {
            Id = details.Id,
            Code = details.Code.Value,
            Name = details.Name,
            UnitOfMeasureId = details.UnitOfMeasureId,
            UnitOfMeasureName = details.UnitOfMeasure != null ? details.UnitOfMeasure.Name : string.Empty,            Costs = details.Costs.Adapt<List<MaintainCostDto>>()
        };
    }
}

