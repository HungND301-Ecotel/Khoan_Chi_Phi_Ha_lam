using Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Queries;

public record class GetGroupedTunnelSupportAndDrillingMaterialUnitPriceQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination) : IRequest<PaginationResponse<GroupedTunnelSupportAndDrillingMaterialUnitPriceDto>>;

public class GetGroupedTunnelSupportAndDrillingUnitPriceQueryHandler(
    IReadRepository<TunnelSupportAndDrillingMaterialUnitPrice> repository,
    ICacheService cacheService)
    : IRequestHandler<GetGroupedTunnelSupportAndDrillingMaterialUnitPriceQuery, PaginationResponse<GroupedTunnelSupportAndDrillingMaterialUnitPriceDto>>
{
    private const string CacheSignalKey = "TunnelSupportAndDrillingMaterialUnitPrice";

    public async Task<PaginationResponse<GroupedTunnelSupportAndDrillingMaterialUnitPriceDto>> Handle(
        GetGroupedTunnelSupportAndDrillingMaterialUnitPriceQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"GetGroupedTunnelSupportAndDrillingMaterialUnitPrice:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<GroupedTunnelSupportAndDrillingMaterialUnitPriceDto>>(cacheKey, cancellationToken);
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

        var spec = new TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec(filter, request.Search);
        var allList = await repository.ListAsync(spec, cancellationToken);

        var grouped = allList
            .GroupBy(d => new
            {
                d.Code,
                d.ProcessId,
                d.ProcessName,
                d.PassportId,
                d.PassportName,
                d.HardnessId,
                d.HardnessName,
                d.TechnologyId,
                d.TechnologyName
            })
            .Select(g => new GroupedTunnelSupportAndDrillingMaterialUnitPriceDto
            {
                Id = g.OrderByDescending(p => p.StartMonth).First().Id,
                Code = g.Key.Code,
                ProcessId = g.Key.ProcessId,
                ProcessName = g.Key.ProcessName,
                PassportId = g.Key.PassportId,
                PassportName = g.Key.PassportName,
                HardnessId = g.Key.HardnessId,
                HardnessName = g.Key.HardnessName,
                TechnologyId = g.Key.TechnologyId,
                TechnologyName = g.Key.TechnologyName,
                Periods = g.OrderByDescending(p => p.StartMonth)
                           .Select(p => new MaterialUnitPricePeriodDto
                           {
                               Id = p.Id,
                               StartMonth = p.StartMonth,
                               EndMonth = p.EndMonth,
                               TotalPrice = p.TotalPrice
                           })
                           .ToList()
            })
            .OrderByCodeNatural(x => x.Code)
            .ThenBy(x => x.ProcessName)
            .ToList();

        var totalCount = grouped.Count;
        var pagedData = request.IgnorePagination
            ? grouped
            : grouped.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        var result = new PaginationResponse<GroupedTunnelSupportAndDrillingMaterialUnitPriceDto>(
            pagedData,
            totalCount,
            request.PageIndex,
            request.PageSize);

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}
