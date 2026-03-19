using Application.Catalog.Index.StoneClampRatio.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.StoneClampRatio;
using MediatR;

namespace Application.Catalog.Index.StoneClampRatio.Queries;

public record class GetAllStoneClampRatioQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<StoneClampRatioDto>>;

public class GetAllStoneClampRatioQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.StoneClampRatio> stoneClampRatioRepository) : IRequestHandler<GetAllStoneClampRatioQuery, PaginationResponse<StoneClampRatioDto>>
{
    public async Task<PaginationResponse<StoneClampRatioDto>> Handle(GetAllStoneClampRatioQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new StoneClampRatiosByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: stoneClampRatioRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.ProcessCode).ThenBy(d => d.Value).ToList();
        return result;
    }
}
