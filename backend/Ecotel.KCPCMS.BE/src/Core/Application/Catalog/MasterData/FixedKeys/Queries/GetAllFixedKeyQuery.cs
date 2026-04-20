using Application.Catalog.MasterData.FixedKeys.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.MasterData;
using Domain.Common.Enums;
using Domain.Entities.MasterData;
using MediatR;

namespace Application.Catalog.MasterData.FixedKeys.Queries;

public record GetAllFixedKeyQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination,
    FixedKeyType? Type) : IRequest<PaginationResponse<FixedKeyDto>>;

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

        var spec = new FixedKeysByPaginationSpec(filter, request.Search, request.Type);
        var result = await paginationService.PaginatedListAsync(
            repository: fixedKeyRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(x => x.Type).ThenBy(x => x.Code).ToList();
        return result;
    }
}