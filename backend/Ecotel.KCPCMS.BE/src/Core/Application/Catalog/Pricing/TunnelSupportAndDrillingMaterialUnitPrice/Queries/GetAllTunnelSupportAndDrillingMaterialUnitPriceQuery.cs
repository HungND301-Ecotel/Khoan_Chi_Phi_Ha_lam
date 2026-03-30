using Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Queries;
public record class GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<MaterialUnitPriceDto>>;

public class GetAllTunnelSupportAndDrillingUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<TunnelSupportAndDrillingMaterialUnitPrice> maintainUnitPriceRepository)
    : IRequestHandler<GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery, PaginationResponse<MaterialUnitPriceDto>>
{
    public async Task<PaginationResponse<MaterialUnitPriceDto>> Handle(GetAllTunnelSupportAndDrillingMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: maintainUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}

