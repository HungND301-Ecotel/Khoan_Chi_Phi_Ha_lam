// File: Application/Catalog/Part/Specifications/PartsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Part;
using Ardalis.Specification;
using Domain.Common.Enums;

namespace Application.Catalog.Index.Part.Specifications;

public class PartsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Part, PartDto>
{
    public PartsByPaginationSpec(PaginationFilter filter, string? search, DateTime date) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();
        var checkDate = new DateOnly(date.Year, date.Month, 1);

        Query
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Equipment).ThenInclude(p => p.Code)
            .Include(p => p.Code)
            .Include(p => p.Costs)
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.Code.Value.ToLower().Contains(searchTerm));
        Query
            .Select(p => new PartDto
            {
                Id = p.Id,
                Code = p.Code.Value,
                Name = p.Name,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureName = p.UnitOfMeasure != null ? p.UnitOfMeasure.Name : string.Empty,
                EquipmentId = p.EquipmentId,
                EquipmentCode = p.Equipment != null ? p.Equipment.Code.Value : string.Empty,
                CostAmount = p.Costs
                    .Where(c => c.CostType == CostType.Part &&
                                c.StartMonth <= checkDate &&
                                c.EndMonth >= checkDate)
                    .Select(c => c.Amount)
                    .FirstOrDefault()
            });
    }
}