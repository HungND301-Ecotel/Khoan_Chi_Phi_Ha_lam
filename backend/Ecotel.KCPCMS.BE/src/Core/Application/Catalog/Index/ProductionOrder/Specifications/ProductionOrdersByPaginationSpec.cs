using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.ProductionOrder;
using Ardalis.Specification;

namespace Application.Catalog.Index.Passport.Specifications;

public class ProductionOrdersByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.ProductionOrder, ProductionOrderDto>
{
    public ProductionOrdersByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(p => p.Code)
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm));
        Query
            .Select(p => new ProductionOrderDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code.Value,
                StartMonth = p.StartMonth,
                EndMonth = p.EndMonth
            });
    }
}