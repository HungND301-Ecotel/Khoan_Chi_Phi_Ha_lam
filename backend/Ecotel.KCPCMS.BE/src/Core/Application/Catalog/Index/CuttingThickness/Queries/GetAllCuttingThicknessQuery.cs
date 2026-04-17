using Application.Catalog.Index.CuttingThickness.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.CuttingThickness;
using MediatR;

namespace Application.Catalog.Index.CuttingThickness.Queries;

public record class GetAllCuttingThicknessQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<CuttingThicknessDto>>;

public class GetAllCuttingThicknessQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.CuttingThickness> cuttingThicknessRepository) : IRequestHandler<GetAllCuttingThicknessQuery, PaginationResponse<CuttingThicknessDto>>
{
    public async Task<PaginationResponse<CuttingThicknessDto>> Handle(GetAllCuttingThicknessQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new CuttingThicknessByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: cuttingThicknessRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.Value).ToList();
        return result;
    }
}
