// File: Application/Catalog/Metrics/Specifications/MetricsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Metric;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Metrics.Specifications;

public class MetricsByPaginationSpec<TEntity>
    : EntitiesByPaginationFilterSpec<TEntity, MetricDto>
    where TEntity : class
{
    public MetricsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            Query.Where(e =>
                EF.Property<string>(e, "Value") != null &&
                EF.Property<string>(e, "Value").ToLower().Contains(searchTerm)
            );
        }

        Query.Select(e => new MetricDto
        {
            Id = EF.Property<DefaultIdType>(e, "Id"),
            Value = EF.Property<string>(e, "Value")
        });
    }
}