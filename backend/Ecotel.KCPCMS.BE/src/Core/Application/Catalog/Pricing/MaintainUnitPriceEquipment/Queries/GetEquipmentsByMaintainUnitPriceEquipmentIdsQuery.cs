using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;

public record GetEquipmentsByMaintainUnitPriceEquipmentIdsQuery(
    IList<Guid> MaintainUnitPriceEquipmentIds,
    DateTime? Date = null) : IRequest<IList<MaintainUnitPriceEquipmentEquipmentsDto>>;

public class GetEquipmentsByMaintainUnitPriceEquipmentIdsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEquipmentsByMaintainUnitPriceEquipmentIdsQuery, IList<MaintainUnitPriceEquipmentEquipmentsDto>>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPriceEquipment>();

    public async Task<IList<MaintainUnitPriceEquipmentEquipmentsDto>> Handle(
        GetEquipmentsByMaintainUnitPriceEquipmentIdsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.MaintainUnitPriceEquipmentIds == null || request.MaintainUnitPriceEquipmentIds.Count == 0)
        {
            return [];
        }

        var normalizedIds = request.MaintainUnitPriceEquipmentIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return [];
        }

        var maintainItems = await _maintainUnitPriceEquipmentRepository.GetAllAsync(
            predicate: x => normalizedIds.Contains(x.Id),
            include: q => q
                .Include(x => x.Part)
                    .ThenInclude(p => p.EquipmentParts)
                        .ThenInclude(ep => ep.Equipment)
                            .ThenInclude(e => e.Code)
                .Include(x => x.Part)
                    .ThenInclude(p => p.EquipmentParts)
                        .ThenInclude(ep => ep.Equipment)
                            .ThenInclude(e => e.UnitOfMeasure)
                .Include(x => x.Part)
                    .ThenInclude(p => p.EquipmentParts)
                        .ThenInclude(ep => ep.Equipment)
                            .ThenInclude(e => e.Costs),
            disableTracking: true);

        var checkDate = request.Date.HasValue
            ? new DateOnly(request.Date.Value.Year, request.Date.Value.Month, 1)
            : new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return normalizedIds.Select(id =>
        {
            var maintainItem = maintainItems.FirstOrDefault(x => x.Id == id);

            var equipments = maintainItem?.Part?.EquipmentParts
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
                .ToList() ?? new List<EquipmentDto>();

            return new MaintainUnitPriceEquipmentEquipmentsDto
            {
                MaintainUnitPriceEquipmentId = id,
                Equipments = equipments
            };
        }).ToList();
    }
}
