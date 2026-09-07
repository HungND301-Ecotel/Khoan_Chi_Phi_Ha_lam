using Application.Catalog.Pricing.SlideUnitPrice.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.SlideUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.SlideUnitPrice.Queries;

public record class GetGroupedSlideUnitPriceQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination) : IRequest<PaginationResponse<GroupedSlideUnitPriceDto>>;

public class GetGroupedSlideUnitPriceQueryHandler(
    IReadRepository<Domain.Entities.Pricing.SlideUnitPrice> repository,
    ICacheService cacheService)
    : IRequestHandler<GetGroupedSlideUnitPriceQuery, PaginationResponse<GroupedSlideUnitPriceDto>>
{
    private const string CacheSignalKey = "SlideUnitPrice";

    public async Task<PaginationResponse<GroupedSlideUnitPriceDto>> Handle(GetGroupedSlideUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetGroupedSlideUnitPrice:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<GroupedSlideUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var filter = new PaginationFilter
        {
            PageNumber = 1,
            PageSize = int.MaxValue,
            IgnorePagination = true
        };

        var spec = new SlideUnitPricesByPaginationSpec(filter, request.Search);
        var allList = await repository.ListAsync(spec, cancellationToken);

        var grouped = allList
            .GroupBy(d => new
            {
                d.Code,
                d.ProcessGroupId,
                d.ProcessGroupName,
                d.PassportId,
                d.PassportName,
                d.HardnessId,
                d.HardnessName
            })
            .Select(g => new GroupedSlideUnitPriceDto
            {
                Id = g.OrderByDescending(p => p.StartMonth).First().Id,
                Code = g.Key.Code,
                ProcessGroupId = g.Key.ProcessGroupId,
                ProcessGroupName = g.Key.ProcessGroupName,
                PassportId = g.Key.PassportId,
                PassportName = g.Key.PassportName,
                HardnessId = g.Key.HardnessId,
                HardnessName = g.Key.HardnessName,
                Periods = g.OrderByDescending(p => p.StartMonth)
                           .Select(p => new SlideUnitPricePeriodDto
                           {
                               Id = p.Id,
                               StartMonth = p.StartMonth,
                               EndMonth = p.EndMonth,
                               TotalPrice = p.TotalPrice
                           })
                           .ToList()
            })
            .OrderByCodeNatural(x => x.Code)
            .ThenBy(x => x.ProcessGroupName)
            .ToList();

        var totalCount = grouped.Count;
        var pagedData = request.IgnorePagination
            ? grouped
            : grouped.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        var result = new PaginationResponse<GroupedSlideUnitPriceDto>(
            pagedData,
            totalCount,
            request.PageIndex,
            request.PageSize);

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}
