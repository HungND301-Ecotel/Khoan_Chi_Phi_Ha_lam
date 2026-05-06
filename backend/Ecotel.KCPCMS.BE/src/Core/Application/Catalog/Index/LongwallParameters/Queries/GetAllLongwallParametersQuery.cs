using Application.Catalog.Index.LongwallParameters.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.LongwallParameters;
using MediatR;

namespace Application.Catalog.Index.LongwallParameters.Queries;
public record class GetAllLongwallParametersQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<LongwallParametersDto>>;

public class GetAllLongwallParametersQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.LongwallParameters> longwallParametersRepository) : IRequestHandler<GetAllLongwallParametersQuery, PaginationResponse<LongwallParametersDto>>
{
    public async Task<PaginationResponse<LongwallParametersDto>> Handle(GetAllLongwallParametersQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new LongwallParametersByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: longwallParametersRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.Llc).ThenBy(d => d.Lkc).ThenBy(d => d.Mk).ToList();
        return result;
    }
}
