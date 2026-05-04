// File: Application/Catalog/ProcessGroups/Specifications/ProcessGroupsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.ProcessGroup;
using Ardalis.Specification;
using Domain.Entities.Index;

namespace Application.Catalog.Index.ProcessGroups.Specifications;

public class ProcessGroupsByPaginationSpec : EntitiesByPaginationFilterSpec<ProcessGroup, ProcessGroupDto>
{
    public ProcessGroupsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(pg => pg.Code)
            .Include(pg => pg.FixedKey)
            .Where(pg => string.IsNullOrWhiteSpace(searchTerm) ||
                         pg.Name.ToLower().Contains(searchTerm) ||
                         (pg.FixedKey != null ? pg.FixedKey.Key : pg.Code.Value).ToLower().Contains(searchTerm));
        Query
        .Select(pg => new ProcessGroupDto
        {
            Id = pg.Id,
            FixedKeyId = pg.FixedKeyId,
            Code = pg.FixedKey != null ? pg.FixedKey.Key : pg.Code.Value,
            Type = pg.FixedKey != null ? pg.FixedKey.Type : pg.Type,
            Name = pg.Name,
        });
    }
}