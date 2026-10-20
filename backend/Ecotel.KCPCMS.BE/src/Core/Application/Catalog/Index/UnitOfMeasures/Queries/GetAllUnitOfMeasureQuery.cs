using Application.Catalog.Index.UnitOfMeasures.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.UnitOfMeasures.Queries;

public record GetAllUnitOfMeasureQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<UnitOfMeasureDto>>;

public class GetAllUnitOfMeasureQueryHandler(IPaginationService paginationService, IReadRepository<UnitOfMeasure> unitOfMeasureRepository) : IRequestHandler<GetAllUnitOfMeasureQuery, PaginationResponse<UnitOfMeasureDto>>
{
    public async Task<PaginationResponse<UnitOfMeasureDto>> Handle(GetAllUnitOfMeasureQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            // Lấy toàn bộ rồi sắp xếp, cắt trang trong bộ nhớ — xem ToPaginationResponse
            IgnorePagination = true
        };

        var spec = new UnitOfMeasuresByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: unitOfMeasureRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        result = result.Data.OrderBy(d => d.Name).ToPaginationResponse(request.PageIndex, request.PageSize, request.IgnorePagination);

        return result;
    }
}

