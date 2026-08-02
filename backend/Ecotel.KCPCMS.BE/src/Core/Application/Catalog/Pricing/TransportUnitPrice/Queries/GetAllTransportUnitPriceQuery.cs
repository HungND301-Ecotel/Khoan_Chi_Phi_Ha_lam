using Application.Catalog.Pricing.TransportUnitPrice.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.TransportUnitPrice;
using MediatR;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Queries;

public record GetAllTransportUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<TransportUnitPriceDto>>;

public class GetAllTransportUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<TransportUnitPriceEntity> transportUnitPriceRepository) : IRequestHandler<GetAllTransportUnitPriceQuery, PaginationResponse<TransportUnitPriceDto>>
{
    public async Task<PaginationResponse<TransportUnitPriceDto>> Handle(GetAllTransportUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new TransportUnitPricesByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: transportUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        return result;
    }
}