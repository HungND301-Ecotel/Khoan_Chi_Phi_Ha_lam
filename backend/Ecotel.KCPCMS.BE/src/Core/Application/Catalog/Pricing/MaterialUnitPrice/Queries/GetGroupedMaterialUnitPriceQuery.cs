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

public record class GetGroupedMaterialUnitPriceQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination,
    TunnelExcavationTrimingUnitPriceType Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation) : IRequest<PaginationResponse<GroupedMaterialUnitPriceDto>>;

public class GetGroupedMaterialUnitPriceQueryHandler(
    IReadRepository<TunnelExcavationMaterialUnitPrice> repository,
    ICacheService cacheService)
    : IRequestHandler<GetGroupedMaterialUnitPriceQuery, PaginationResponse<GroupedMaterialUnitPriceDto>>
{
    private const string CacheSignalKey = "MaterialUnitPrice";

    public async Task<PaginationResponse<GroupedMaterialUnitPriceDto>> Handle(GetGroupedMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetGroupedMaterialUnitPrice:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}:{request.Type}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<GroupedMaterialUnitPriceDto>>(cacheKey, cancellationToken);
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

        var spec = new MaterialUnitPricesByPaginationSpec(filter, request.Search, request.Type);
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
                d.InsertItemId,
                d.InsertItemName,
                d.SupportStepId,
                d.SupportStepName,
                d.Type
            })
            .Select(g => new GroupedMaterialUnitPriceDto
            {
                Id = g.OrderByDescending(p => p.StartMonth).First().Id,
                Code = g.Key.Code,
                ProcessId = g.Key.ProcessId,
                ProcessName = g.Key.ProcessName,
                PassportId = g.Key.PassportId,
                PassportName = g.Key.PassportName,
                HardnessId = g.Key.HardnessId,
                HardnessName = g.Key.HardnessName,
                InsertItemId = g.Key.InsertItemId,
                InsertItemName = g.Key.InsertItemName,
                SupportStepId = g.Key.SupportStepId,
                SupportStepName = g.Key.SupportStepName,
                Type = g.Key.Type,
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

        var result = new PaginationResponse<GroupedMaterialUnitPriceDto>(
            pagedData,
            totalCount,
            request.PageIndex,
            request.PageSize);

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}
