using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.StoneClampRatio;
using Ardalis.Specification;

namespace Application.Catalog.Index.StoneClampRatio.Specifications;

public class StoneClampRatiosByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.StoneClampRatio, StoneClampRatioDto>
{
    public StoneClampRatiosByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(s => string.IsNullOrWhiteSpace(searchTerm) ||
                        s.Value.ToLower().Contains(searchTerm));
        Query
            .Select(s => new StoneClampRatioDto
            {
                Id = s.Id,
                Value = s.Value
            });
    }
}