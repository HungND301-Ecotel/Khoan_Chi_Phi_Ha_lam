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

public record GetPartByIdQuery(DefaultIdType Id) : IRequest<PartDetailDto>;

public class GetPartByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPartByIdQuery, PartDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    public async Task<PartDetailDto> Handle(GetPartByIdQuery request, CancellationToken cancellationToken)
    {
        var details = await _partRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(t => t.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
                .Include(t => t.UnitOfMeasure)
                .Include(t => t.Costs)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new PartDetailDto
        {
            Id = details.Id,
            Code = details.Code.Value,
            Name = details.Name,
            EquipmentIds = details.EquipmentParts.Select(e => e.EquipmentId).ToList(),
            EquipmentCodes = details.EquipmentParts
                .Where(e => e.Equipment?.Code != null)
                .Select(e => e.Equipment!.Code!.Value)
                .OrderBy(code => code)
                .ToList(),
            ReplacementTimeStandard = details.ReplacementTimeStandard,
            UnitOfMeasureId = details.UnitOfMeasureId,
            UnitOfMeasureName = details.UnitOfMeasure != null ? details.UnitOfMeasure.Name : string.Empty,
            Costs = details.Costs.Adapt<List<MaintainCostDto>>()
        };
    }
}
