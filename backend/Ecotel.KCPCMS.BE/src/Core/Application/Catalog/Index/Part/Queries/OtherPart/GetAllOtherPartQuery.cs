using Application.Catalog.Index.Part.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Part;
using MediatR;

namespace Application.Catalog.Index.Part.Queries.Part;

public record GetAllOtherPartQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination, DateTime Date) : IRequest<PaginationResponse<OtherPartDto>>;

public class GetAllOtherPartQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Index.Part> partRepository) : IRequestHandler<GetAllOtherPartQuery, PaginationResponse<OtherPartDto>>
{
    public async Task<PaginationResponse<OtherPartDto>> Handle(GetAllOtherPartQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new OtherPartsByPaginationSpec(filter, request.Search, request.Date);

        var result = await paginationService.PaginatedListAsync(
            repository: partRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(d => d.Code).ThenBy(d => d.Name).ToList();
        return result;
    }
}
