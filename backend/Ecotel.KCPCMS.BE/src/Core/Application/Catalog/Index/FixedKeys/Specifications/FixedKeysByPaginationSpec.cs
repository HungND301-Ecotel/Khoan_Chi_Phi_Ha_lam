using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.FixedKey;
using Ardalis.Specification;
using Domain.Entities.Index;

namespace Application.Catalog.Index.FixedKeys.Specifications;

public class FixedKeysByPaginationSpec : EntitiesByPaginationFilterSpec<FixedKey, FixedKeyDto>
{
    public FixedKeysByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? string.Empty).Trim().ToLower();

        Query
            .Where(fk => string.IsNullOrWhiteSpace(searchTerm)
                || fk.Name.ToLower().Contains(searchTerm)
                || fk.Key.ToLower().Contains(searchTerm));

        Query.Select(fk => new FixedKeyDto
        {
            Id = fk.Id,
            Key = fk.Key,
            Name = fk.Name,
            Type = fk.Type,
        });
    }
}