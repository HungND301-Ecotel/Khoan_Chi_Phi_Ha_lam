using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using Domain.Entities.Index;
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
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();

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
                    .ThenInclude(p => p.AssignmentCodeMaterials),
            disableTracking: true);

        var assignmentCodeIds = maintainItems
            .SelectMany(x => x.Part?.AssignmentCodeMaterials.Select(acm => acm.AssignmentCodeId) ?? [])
            .Distinct()
            .ToList();

        var assignmentCodes = await _assignmentCodeRepository.GetAllAsync(
            predicate: x => assignmentCodeIds.Contains(x.Id),
            include: q => q
                .Include(x => x.Code)
                .Include(x => x.UnitOfMeasure)
                .Include(x => x.Costs),
            disableTracking: true);

        var checkDate = request.Date.HasValue
            ? new DateOnly(request.Date.Value.Year, request.Date.Value.Month, 1)
            : new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return normalizedIds.Select(id =>
        {
            var maintainItem = maintainItems.FirstOrDefault(x => x.Id == id);

            var equipments = assignmentCodes
                .Where(ac => maintainItem?.Part?.AssignmentCodeMaterials.Any(link => link.AssignmentCodeId == ac.Id) == true)
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
