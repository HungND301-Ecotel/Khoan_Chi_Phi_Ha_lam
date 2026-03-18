using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.CuttingThickness;
using Ardalis.Specification;

namespace Application.Catalog.Index.CuttingThickness.Specifications;

public class CuttingThicknessByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.CuttingThickness, CuttingThicknessDto>
{
    public CuttingThicknessByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Value.ToLower().Contains(searchTerm));
        Query
            .Select(p => new CuttingThicknessDto
            {
                Id = p.Id,
                Value = p.Value
            });
    }
}
