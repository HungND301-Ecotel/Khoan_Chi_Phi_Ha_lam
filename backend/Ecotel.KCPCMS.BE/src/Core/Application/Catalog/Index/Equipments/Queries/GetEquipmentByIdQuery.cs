using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Cost;
using Application.Dto.Catalog.Equipment;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Queries;
public record GetEquipmentByIdQuery(DefaultIdType Id) : IRequest<EquipmentDetailDto>;

public class GetEquipmentByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEquipmentByIdQuery, EquipmentDetailDto>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    public async Task<EquipmentDetailDto> Handle(GetEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var detail = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(c => c.UnitOfMeasure)
                .Include(c => c.Costs)
                .Include(c => c.Code)
                .Include(c => c.EquipmentParts)
                    .ThenInclude(c => c.Part)
                    .ThenInclude(c => c.Code)
                .Include(c => c.EquipmentProcessGroups)
                    .ThenInclude(c => c.ProcessGroup)
                    .ThenInclude(c => c.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);


        return new EquipmentDetailDto
        {
            Id = detail.Id,
            Code = detail.Code?.Value ?? "",
            Name = detail.Name,
            UnitOfMeasureId = detail.UnitOfMeasureId,
            UnitOfMeasureName = detail.UnitOfMeasure != null ? detail.UnitOfMeasure.Name : string.Empty,
            Costs = detail.Costs.Adapt<List<ElectricityCostDto>>(),
            ProcessGroups = detail.EquipmentProcessGroups
                .Where(epg => epg.ProcessGroup?.Code != null)
                .Select(epg => new EquipmentProcessGroupDto
                {
                    Id = epg.ProcessGroupId,
                    Code = epg.ProcessGroup!.Code!.Value,
                    Name = epg.ProcessGroup.Name
                })
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ToList(),
            ProcessGroupId = detail.EquipmentProcessGroups
                .Select(epg => (Guid?)epg.ProcessGroupId)
                .FirstOrDefault(),
            PartIds = detail.EquipmentParts
                .Select(ep => ep.PartId)
                .Distinct()
                .ToList(),
            Parts = detail.EquipmentParts
                .Where(ep => ep.Part?.Code != null)
                .Select(ep => new EquipmentPartDto
                {
                    Id = ep.PartId,
                    Code = ep.Part!.Code!.Value,
                    Name = ep.Part.Name
                })
                .OrderBy(p => p.Code)
                .ThenBy(p => p.Name)
                .ToList()
        };
    }
}
