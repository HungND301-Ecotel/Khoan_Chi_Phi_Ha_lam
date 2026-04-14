using Application.Catalog.Index.AkFactorConfig.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.AkFactorConfig;
using MediatR;

namespace Application.Catalog.Index.AkFactorConfig.Queries;

public record GetAllAkFactorConfigQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<AkFactorConfigDto>>;

public class GetAllAkFactorConfigQueryHandler(
    IPaginationService paginationService,
    IReadRepository<Domain.Entities.Index.AkFactorConfig> AkFactorConfigRepository)
    : IRequestHandler<GetAllAkFactorConfigQuery, PaginationResponse<AkFactorConfigDto>>
{
    public async Task<PaginationResponse<AkFactorConfigDto>> Handle(GetAllAkFactorConfigQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new AkFactorConfigsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: AkFactorConfigRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data
            .OrderBy(d => d.ProcessGroupCode)
            .ThenBy(d => d.MinAkDiff ?? decimal.MinValue)
            .ThenBy(d => d.MaxAkDiff ?? decimal.MaxValue)
            .ToList();
        return result;
    }
}
