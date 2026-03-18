// File: Application/Catalog/LongwallParameters/Specifications/LongwallParametersByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.LongwallParameters;
using Ardalis.Specification;

namespace Application.Catalog.Index.LongwallParameters.Specifications;

public class LongwallParametersByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.LongwallParameters, LongwallParametersDto>
{
    public LongwallParametersByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Llc.ToLower().Contains(searchTerm));
        Query
            .Select(p => new LongwallParametersDto
            {
                Id = p.Id,
                Llc = p.Llc,
                Lkc = p.Lkc,
                Mk = p.Mk
            });
    }
}
