using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Queries.Part;

public record GetAllPartByEquipmentIdQuery(DefaultIdType EquipmentId) : IRequest<IList<PartDetailBaseDto>>;

public class GetAllPartByEquipmentIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllPartByEquipmentIdQuery, IList<PartDetailBaseDto>>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    public async Task<IList<PartDetailBaseDto>> Handle(GetAllPartByEquipmentIdQuery request, CancellationToken cancellationToken)
    {
        var details = await _partRepository.GetAllAsync(
            predicate: t => t.EquipmentParts.Any(e => e.EquipmentId == request.EquipmentId),
            include: t => t
                .Include(t => t.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
                .Include(t => t.PartProcessGroups).ThenInclude(ppg => ppg.ProcessGroup).ThenInclude(pg => pg.Code)
                .Include(t => t.UnitOfMeasure)
                .Include(t => t.Costs)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var curMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return details.Select(partDetail => new PartDetailBaseDto
        {
            Id = partDetail.Id,
            Code = partDetail.Code.Value,
            Name = partDetail.Name,
            PartType = partDetail.Type,
            EquipmentIds = partDetail.EquipmentParts.Select(e => e.EquipmentId).ToList(),
            EquipmentCodes = partDetail.EquipmentParts
                .Where(e => e.Equipment?.Code != null)
                .Select(e => e.Equipment!.Code!.Value)
                .OrderBy(code => code)
                .ToList(),
            UnitOfMeasureId = partDetail.UnitOfMeasureId,
            UnitOfMeasureName = partDetail.UnitOfMeasure != null ? partDetail.UnitOfMeasure.Name : string.Empty,
            ProcessGroups = partDetail.PartProcessGroups
                .Where(ppg => ppg.ProcessGroup?.Code != null)
                .Select(ppg => new PartProcessGroupDto
                {
                    Id = ppg.ProcessGroupId,
                    Code = ppg.ProcessGroup!.Code!.Value,
                    Name = ppg.ProcessGroup.Name
                })
                .DistinctBy(pg => pg.Id)
                .OrderByCodeNatural(pg => pg.Code)
                .ThenBy(pg => pg.Name)
                .ToList(),
            CurrentCost = partDetail.Costs.FirstOrDefault(c => c.StartMonth <= curMonth && c.EndMonth >= curMonth)?.Amount ?? 0,
            ActualAmount = partDetail.Costs.FirstOrDefault(c => c.StartMonth <= curMonth && c.EndMonth >= curMonth)?.ActualAmount ?? 0
        }).OrderByCodeNatural(p => p.Code).ThenBy(p => p.Name).ToList();
    }
}

