using Application.Catalog.Index.Product.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Product;
using MediatR;

namespace Application.Catalog.Index.Product.Queries;

public record class GetAllProductQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ProductDto>>;

public class GetAllProductQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.Product> productRepository)
    : IRequestHandler<GetAllProductQuery, PaginationResponse<ProductDto>>
{
    public async Task<PaginationResponse<ProductDto>> Handle(GetAllProductQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new ProductsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: productRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.Code).ThenBy(d => d.Name).ToList();
        return result;
    }

}