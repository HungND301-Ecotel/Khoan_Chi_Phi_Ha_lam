using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Queries;

public record GetEquipmentsByPartIdQuery(DefaultIdType PartId, DateTime Date) : IRequest<IList<EquipmentDto>>;

public class GetEquipmentsByPartIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEquipmentsByPartIdQuery, IList<EquipmentDto>>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Part>();

    public async Task<IList<EquipmentDto>> Handle(GetEquipmentsByPartIdQuery request, CancellationToken cancellationToken)
    {
        var part = await _partRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.PartId,
            include: p => p
                .Include(x => x.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment)
                        .ThenInclude(e => e.Code)
                .Include(x => x.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment)
                        .ThenInclude(e => e.UnitOfMeasure)
                .Include(x => x.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment)
                        .ThenInclude(e => e.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var checkDate = new DateOnly(request.Date.Year, request.Date.Month, 1);

        return part.EquipmentParts
            .Where(ep => ep.Equipment != null)
            .Select(ep => ep.Equipment!)
            .GroupBy(e => e.Id)
            .Select(group =>
            {
                var equipment = group.First();
                return new EquipmentDto
                {
                    Id = equipment.Id,
                    Code = equipment.Code?.Value ?? string.Empty,
                    Name = equipment.Name,
                    UnitOfMeasureId = equipment.UnitOfMeasureId,
                    UnitOfMeasureName = equipment.UnitOfMeasure != null ? equipment.UnitOfMeasure.Name : string.Empty,
                    CurrentPrice = equipment.GetEffectiveDateCost(checkDate)
                };
            })
            .OrderBy(e => e.Code)
            .ThenBy(e => e.Name)
            .ToList();
    }
}
