using Application.Catalog.Pricing.SlideUnitPrice.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.SlideUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.SlideUnitPrice.Queries;
public record class GetAllSlideUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<SlideUnitPriceDto>>;

public class GetAllUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.SlideUnitPrice> slideUnitPriceRepository, ICacheService cacheService) : IRequestHandler<GetAllSlideUnitPriceQuery, PaginationResponse<SlideUnitPriceDto>>
{
    private const string CacheSignalKey = "SlideUnitPrice";

    public async Task<PaginationResponse<SlideUnitPriceDto>> Handle(GetAllSlideUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetAllSlideUnitPrice:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<SlideUnitPriceDto>>(cacheKey, cancellationToken);
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

        var spec = new SlideUnitPricesByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: slideUnitPriceRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(d => d.ProcessGroupName).ToList();
        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}
