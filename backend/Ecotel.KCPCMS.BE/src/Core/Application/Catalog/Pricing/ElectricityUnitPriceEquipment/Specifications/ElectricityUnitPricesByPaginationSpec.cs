// File: Application/Catalog/Pricing/ElectricityUnitPriceEquipment/Specifications/ElectricityUnitPricesByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Specifications;

public sealed class ElectricityUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>
{
    public ElectricityUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(e => e.Equipment!).ThenInclude(eq => eq!.UnitOfMeasure)
            .Include(e => e.Equipment!).ThenInclude(eq => eq!.Costs)
            .Include(e => e.Equipment!).ThenInclude(eq => eq!.Code)
            .Where(e => e.Equipment != null &&
                        (string.IsNullOrWhiteSpace(searchTerm) ||
                         e.Equipment.Name.ToLower().Contains(searchTerm) ||
                         e.Equipment.Code.Value.ToLower().Contains(searchTerm)));
    }
}