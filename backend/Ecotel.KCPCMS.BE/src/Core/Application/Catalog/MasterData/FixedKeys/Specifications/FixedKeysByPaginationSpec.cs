using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MasterData;
using Ardalis.Specification;
using Domain.Common.Enums;
using Domain.Entities.MasterData;

namespace Application.Catalog.MasterData.FixedKeys.Specifications;

public class FixedKeysByPaginationSpec : EntitiesByPaginationFilterSpec<FixedKey, FixedKeyDto>
{
    public FixedKeysByPaginationSpec(PaginationFilter filter, string? search, FixedKeyType? type) : base(filter)
    {
        var searchTerm = (search ?? string.Empty).Trim().ToLower();

        Query.Where(x => (!type.HasValue || x.Type == type.Value) &&
                         (string.IsNullOrWhiteSpace(searchTerm) ||
                          x.Code.ToLower().Contains(searchTerm) ||
                          x.Name.ToLower().Contains(searchTerm)));

        Query.Select(x => new FixedKeyDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Type = x.Type,
            IsSystem = x.IsSystem,
        });
    }
}