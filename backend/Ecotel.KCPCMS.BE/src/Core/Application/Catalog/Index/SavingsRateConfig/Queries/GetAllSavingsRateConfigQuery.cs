using Application.Catalog.Index.SavingsRateConfig.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.SavingsRateConfig;
using MediatR;

namespace Application.Catalog.Index.SavingsRateConfig.Queries;

public record GetAllSavingsRateConfigQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<SavingsRateConfigDto>>;

public class GetAllSavingsRateConfigQueryHandler(
    IPaginationService paginationService,
    IReadRepository<Domain.Entities.Index.SavingsRateConfig> savingsRateConfigRepository)
    : IRequestHandler<GetAllSavingsRateConfigQuery, PaginationResponse<SavingsRateConfigDto>>
{
    public async Task<PaginationResponse<SavingsRateConfigDto>> Handle(GetAllSavingsRateConfigQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new SavingsRateConfigsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: savingsRateConfigRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data
            .OrderBy(d => d.MinRevenue ?? decimal.MinValue)
            .ThenBy(d => d.MaxRevenue ?? decimal.MaxValue)
            .ToList();
        return result;
    }
}
