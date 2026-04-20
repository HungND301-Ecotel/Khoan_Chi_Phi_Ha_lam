// File: Application/Catalog/ProcessGroups/Specifications/ProcessGroupsByPaginationSpec.cs
using Application.Catalog.MasterData.FixedKeys;
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
            .Include(pg => pg.FixedKey)
            .Include(pg => pg.Code)
            .Where(pg => string.IsNullOrWhiteSpace(searchTerm) ||
                         pg.Name.ToLower().Contains(searchTerm) ||
                         pg.Code.Value.ToLower().Contains(searchTerm) ||
                         (pg.FixedKey != null && pg.FixedKey.Code.ToLower().Contains(searchTerm)) ||
                         (pg.FixedKey != null && pg.FixedKey.Name.ToLower().Contains(searchTerm)));
        Query
        .Select(pg => new ProcessGroupDto
        {
            Id = pg.Id,
            Code = pg.Code.Value,
            Type = FixedKeyCodeMapper.ResolveProcessGroupType(pg.FixedKey),
            Name = pg.Name,
            FixedKeyId = pg.FixedKeyId,
            FixedKeyCode = pg.FixedKey != null ? pg.FixedKey.Code : null,
            FixedKeyName = pg.FixedKey != null ? pg.FixedKey.Name : null,
        });
    }
}