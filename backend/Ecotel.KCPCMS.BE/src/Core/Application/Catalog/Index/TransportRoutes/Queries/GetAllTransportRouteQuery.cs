using Application.Catalog.Index.TransportRoutes.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.TransportRoute;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.TransportRoutes.Queries;

public record GetAllTransportRouteQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<TransportRouteDto>>;

public class GetAllTransportRouteQueryHandler(IPaginationService paginationService, IReadRepository<TransportRoute> transportRouteRepository) : IRequestHandler<GetAllTransportRouteQuery, PaginationResponse<TransportRouteDto>>
{
    public async Task<PaginationResponse<TransportRouteDto>> Handle(GetAllTransportRouteQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new TransportRoutesByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: transportRouteRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        result.Data = result.Data.OrderBy(d => d.Code).ToList();

        return result;
    }
}