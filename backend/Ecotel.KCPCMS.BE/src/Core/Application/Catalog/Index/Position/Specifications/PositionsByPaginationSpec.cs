using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Position;
using Ardalis.Specification;

namespace Application.Catalog.Index.Position.Specifications;

public class PositionsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Position, PositionDto>
{
    public PositionsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm));

        Query
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Name = p.Name,
                Level = p.Level ?? 0,
                Description = p.Description
            });
    }
}

