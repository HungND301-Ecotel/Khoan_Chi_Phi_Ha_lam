// File: Application/Catalog/UnitOfMeasures/Specifications/UnitOfMeasuresByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.UnitOfMeasure;
using Ardalis.Specification;
using Domain.Entities.Index;

namespace Application.Catalog.Index.UnitOfMeasures.Specifications;

public class UnitOfMeasuresByPaginationSpec : EntitiesByPaginationFilterSpec<UnitOfMeasure, UnitOfMeasureDto>
{
    public UnitOfMeasuresByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(u => string.IsNullOrWhiteSpace(searchTerm) ||
                        u.Name.ToLower().Contains(searchTerm));
        Query
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                Name = u.Name
            });
    }
}