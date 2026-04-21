using Application.Catalog.Pricing.MaterialUnitPrice.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Queries;

public record class GetAllMaterialUnitPriceQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination,
    TunnelExcavationTrimingUnitPriceType Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation) : IRequest<PaginationResponse<MaterialUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<TunnelExcavationMaterialUnitPrice> maintainUnitPriceRepository, ICacheService cacheService)
    : IRequestHandler<GetAllMaterialUnitPriceQuery, PaginationResponse<MaterialUnitPriceDto>>
{
    private const string CacheSignalKey = "MaterialUnitPrice";

    public async Task<PaginationResponse<MaterialUnitPriceDto>> Handle(GetAllMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetAllMaterialUnitPrice:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}:{request.Type}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<MaterialUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new MaterialUnitPricesByPaginationSpec(filter, request.Search, request.Type);

        var result = await paginationService.PaginatedListAsync(
            repository: maintainUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(d => d.ProcessName).ToList();
        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}
