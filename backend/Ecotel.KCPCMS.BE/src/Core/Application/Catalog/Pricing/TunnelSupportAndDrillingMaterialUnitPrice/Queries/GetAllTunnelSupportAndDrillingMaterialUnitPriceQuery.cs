using Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Queries;

public record class GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<TunnelSupportAndDrillingMaterialUnitPriceDto>>;

public class GetAllTunnelSupportAndDrillingUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<TunnelSupportAndDrillingMaterialUnitPrice> maintainUnitPriceRepository)
    : IRequestHandler<GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery, PaginationResponse<TunnelSupportAndDrillingMaterialUnitPriceDto>>
{
    public async Task<PaginationResponse<TunnelSupportAndDrillingMaterialUnitPriceDto>> Handle(GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: maintainUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.Code).ThenBy(d => d.ProcessName).ToList();
        return result;
    }
}

