// File: Application/Catalog/Product/Specifications/ProductsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Product;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Product.Specifications;

public class ProductsByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Product, ProductDto>
{
    public ProductsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(p => p.ProcessGroup).ThenInclude(pg => pg.Code)
            .Include(p => p.Code)
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.Code.Value.ToLower().Contains(searchTerm) ||
                        p.ProcessGroup != null && p.ProcessGroup.Name.ToLower().Contains(searchTerm));
        Query
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Code = p.Code.Value,
                Name = p.Name,
                ProcessGroupId = p.ProcessGroupId,
                ProcessGroupCode = p.ProcessGroup != null ? p.ProcessGroup.Code.Value : string.Empty,
                ProcessGroupName = p.ProcessGroup != null ? p.ProcessGroup.Name : string.Empty
            });
    }
}