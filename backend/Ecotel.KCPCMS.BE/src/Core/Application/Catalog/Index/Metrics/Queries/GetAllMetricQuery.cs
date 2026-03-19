using Application.Catalog.Index.Metrics.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Metric;
using Domain.Common.Contracts;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record GetAllMetricQuery<TEntity>(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<MetricDto>>
    where TEntity : class, IAggregateRoot;

public class GetAllMetricQueryHandler<TEntity>(IPaginationService paginationService, IReadRepository<TEntity> repository) : IRequestHandler<GetAllMetricQuery<TEntity>, PaginationResponse<MetricDto>>
    where TEntity : class, IAggregateRoot
{
    public async Task<PaginationResponse<MetricDto>> Handle(GetAllMetricQuery<TEntity> request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new MetricsByPaginationSpec<TEntity>(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: repository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.Value).ToList();
        return result;
    }
}
