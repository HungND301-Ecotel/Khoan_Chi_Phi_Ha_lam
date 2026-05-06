using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;

public record GetEquipmentsByPartIdsQuery(
    IList<Guid> PartIds,
    DateTime? Date = null) : IRequest<IList<PartEquipmentsDto>>;

public class GetEquipmentsByPartIdsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEquipmentsByPartIdsQuery, IList<PartEquipmentsDto>>
{
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();

    public async Task<IList<PartEquipmentsDto>> Handle(
        GetEquipmentsByPartIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PartIds == null || request.PartIds.Count == 0)
        {
            return [];
        }

        var normalizedIds = request.PartIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return [];
        }

        var parts = await _partRepository.GetAllAsync(
            predicate: x => normalizedIds.Contains(x.Id),
            include: q => q
                .Include(x => x.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment)
                        .ThenInclude(e => e.Code)
                .Include(x => x.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment)
                        .ThenInclude(e => e.UnitOfMeasure)
                .Include(x => x.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment)
                        .ThenInclude(e => e.Costs),
            disableTracking: true);

        var checkDate = request.Date.HasValue
            ? new DateOnly(request.Date.Value.Year, request.Date.Value.Month, 1)
            : new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return normalizedIds.Select(partId =>
        {
            var part = parts.FirstOrDefault(x => x.Id == partId);

            var equipments = new List<EquipmentDto>();
            if (part?.Type == PartType.Part)
            {
                equipments = (part.EquipmentParts ?? [])
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
                            UnitOfMeasureName = equipment.UnitOfMeasure?.Name ?? string.Empty,
                            CurrentPrice = equipment.GetEffectiveDateCost(checkDate)
                        };
                    })
                    .OrderBy(e => e.Code)
                    .ThenBy(e => e.Name)
                    .ToList();
            }

            return new PartEquipmentsDto
            {
                PartId = partId,
                Equipments = equipments
            };
        }).ToList();
    }
}
