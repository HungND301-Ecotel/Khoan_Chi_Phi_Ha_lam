using Application.Catalog.Index.TransportLocation.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.TransportLocation;
using MediatR;

namespace Application.Catalog.Index.TransportLocation.Queries;

public record GetAllTransportLocationQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<TransportLocationDto>>;

public class GetAllTransportLocationQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.TransportLocation> transportLocationRepository) : IRequestHandler<GetAllTransportLocationQuery, PaginationResponse<TransportLocationDto>>
{
    public async Task<PaginationResponse<TransportLocationDto>> Handle(GetAllTransportLocationQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };
        var spec = new TransportLocationsByPaginationSpec(filter, request.Search);

        var rawList = await paginationService.PaginatedListAsync(
            repository: transportLocationRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        rawList.Data = rawList.Data.OrderBy(d => d.Name).ToList();

        return rawList;
    }
}