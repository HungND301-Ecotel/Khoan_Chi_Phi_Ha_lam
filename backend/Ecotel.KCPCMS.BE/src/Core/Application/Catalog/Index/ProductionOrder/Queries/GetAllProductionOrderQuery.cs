using Application.Catalog.Index.Passport.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.ProductionOrder;
using MediatR;

namespace Application.Catalog.Index.Passport.Queries;

public record class GetAllProductionOrderQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ProductionOrderDto>>;

public class GetAllProductionOrderQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.ProductionOrder> productionOrderRepository) : IRequestHandler<GetAllProductionOrderQuery, PaginationResponse<ProductionOrderDto>>
{
    public async Task<PaginationResponse<ProductionOrderDto>> Handle(GetAllProductionOrderQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new ProductionOrdersByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: productionOrderRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(d => d.Name).ToList();
        return result;
    }
}
