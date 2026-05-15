using Application.Common.Models;
using Application.Common.Specification;
using Ardalis.Specification;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Specifications;

public sealed class MaintainUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<MaintainUnitPrice>
{
    public MaintainUnitPricesByPaginationSpec(PaginationFilter filter, string? search, MaintainUnitPriceType? type) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.Equipment).ThenInclude(e => e.AssignmentCodeMaterials).ThenInclude(acm => acm.Material).ThenInclude(p => p.UnitOfMeasure)
            .Include(m => m.Equipment).ThenInclude(e => e.Code)
            .Include(m => m.MaintainUnitPriceEquipments).ThenInclude(e => e.Part).ThenInclude(p => p.Costs)
            .Where(m => (string.IsNullOrWhiteSpace(searchTerm) ||
                        m.Equipment!.Code.Value.ToLower().Contains(searchTerm) ||
                        m.Equipment.Name.ToLower().Contains(searchTerm)) &&
                        (!type.HasValue || m.Type == type.Value));
    }
}
