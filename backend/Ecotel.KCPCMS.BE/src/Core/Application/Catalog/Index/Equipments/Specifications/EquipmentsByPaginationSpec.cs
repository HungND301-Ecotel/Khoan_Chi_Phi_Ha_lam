using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Equipment;
using Ardalis.Specification;
using Domain.Entities.Index;

namespace Application.Catalog.Index.Equipments.Specifications;

public class EquipmentsByPaginationSpec : EntitiesByPaginationFilterSpec<Equipment, EquipmentDto>
{
    public EquipmentsByPaginationSpec(PaginationFilter filter, string? search, DateTime date) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();
        var checkDate = new DateOnly(date.Year, date.Month, 1);
        Query
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Costs)
            .Include(x => x.Code)
            .Include(x => x.EquipmentProcessGroups)
                .ThenInclude(x => x.ProcessGroup)
                .ThenInclude(x => x.Code)
            .Where(x => x.Code != null && (string.IsNullOrWhiteSpace(searchTerm) ||
                                           x.Name.ToLower().Contains(searchTerm) ||
                                           x.Code.Value.ToLower().Contains(searchTerm)));

        Query.Select(x => new EquipmentDto
        {
            Id = x.Id,
            Code = x.Code.Value,
            Name = x.Name,
            UnitOfMeasureId = x.UnitOfMeasureId,
            UnitOfMeasureName = x.UnitOfMeasure != null ? x.UnitOfMeasure.Name : string.Empty,
            CurrentPrice = x.GetEffectiveDateCost(checkDate),
            ProcessGroups = x.EquipmentProcessGroups
                .Where(epg => epg.ProcessGroup != null && epg.ProcessGroup.Code != null)
                .Select(epg => new EquipmentProcessGroupDto
                {
                    Id = epg.ProcessGroupId,
                    Code = epg.ProcessGroup!.Code!.Value,
                    Name = epg.ProcessGroup.Name,
                    Type = epg.ProcessGroup.Type
                })
                .OrderBy(epg => epg.Code)
                .ThenBy(epg => epg.Name)
                .ToList()
        });
    }
}
