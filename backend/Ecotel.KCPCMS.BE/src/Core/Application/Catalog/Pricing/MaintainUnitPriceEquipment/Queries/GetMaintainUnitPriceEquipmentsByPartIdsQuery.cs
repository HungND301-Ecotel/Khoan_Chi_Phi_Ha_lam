using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using MediatR;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;

public record GetMaintainUnitPriceEquipmentsByPartIdsQuery(IList<Guid> PartIds)
    : IRequest<IList<PartMaintainUnitPriceEquipmentsDto>>;

public class GetMaintainUnitPriceEquipmentsByPartIdsQueryHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<
        GetMaintainUnitPriceEquipmentsByPartIdsQuery,
        IList<PartMaintainUnitPriceEquipmentsDto>
    >
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPriceEquipment>();

    public async Task<IList<PartMaintainUnitPriceEquipmentsDto>> Handle(
        GetMaintainUnitPriceEquipmentsByPartIdsQuery request,
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

        var maintainUnitPriceEquipments =
            await _maintainUnitPriceEquipmentRepository.GetAllAsync(
                predicate: x => normalizedIds.Contains(x.PartId),
                disableTracking: true);

        var groupedByPartId = maintainUnitPriceEquipments
            .GroupBy(x => x.PartId)
            .ToDictionary(
                group => group.Key,
                group => (IList<Guid>)group.Select(x => x.Id).Distinct().ToList());

        return normalizedIds.Select(partId => new PartMaintainUnitPriceEquipmentsDto
        {
            PartId = partId,
            MaintainUnitPriceEquipmentIds = groupedByPartId.TryGetValue(partId, out var ids)
                ? ids
                : []
        }).ToList();
    }
}

