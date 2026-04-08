using Application.Catalog.Index.RevenueCostAdjustmentConfig.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using MediatR;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Queries;

public record GetAllRevenueCostAdjustmentConfigQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<RevenueCostAdjustmentConfigDto>>;

public class GetAllRevenueCostAdjustmentConfigQueryHandler(
    IPaginationService paginationService,
    IReadRepository<Domain.Entities.Index.RevenueCostAdjustmentConfig> revenueCostAdjustmentConfigRepository)
    : IRequestHandler<GetAllRevenueCostAdjustmentConfigQuery, PaginationResponse<RevenueCostAdjustmentConfigDto>>
{
    public async Task<PaginationResponse<RevenueCostAdjustmentConfigDto>> Handle(GetAllRevenueCostAdjustmentConfigQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new RevenueCostAdjustmentConfigsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: revenueCostAdjustmentConfigRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data
            .OrderBy(d => d.MinProfit ?? decimal.MinValue)
            .ThenBy(d => d.MaxProfit ?? decimal.MaxValue)
            .ToList();

        return result;
    }
}
