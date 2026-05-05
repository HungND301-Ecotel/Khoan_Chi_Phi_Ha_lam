using Application.Catalog.Index.FixedKeys.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.FixedKey;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.FixedKeys.Queries;

public record class GetAllFixedKeyQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination)
    : IRequest<PaginationResponse<FixedKeyDto>>;

public class GetAllFixedKeyQueryHandler(
    IPaginationService paginationService,
    IReadRepository<FixedKey> fixedKeyRepository) : IRequestHandler<GetAllFixedKeyQuery, PaginationResponse<FixedKeyDto>>
{
    public async Task<PaginationResponse<FixedKeyDto>> Handle(GetAllFixedKeyQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
        };

        var spec = new FixedKeysByPaginationSpec(filter, request.Search);
        var result = await paginationService.PaginatedListAsync(
            repository: fixedKeyRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data
            .OrderBy(o => o.Type)
            .ThenBy(o => o.Key)
            .ToList();

        return result;
    }
}