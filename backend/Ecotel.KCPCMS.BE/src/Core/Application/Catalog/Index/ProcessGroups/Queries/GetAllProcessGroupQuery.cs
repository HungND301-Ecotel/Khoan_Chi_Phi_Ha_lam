using Application.Catalog.Index.ProcessGroups.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.ProcessGroup;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.ProcessGroups.Queries;

public record class GetAllProcessGroupQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ProcessGroupDto>>;

public class GetAllProcessGroupQueryHandler(IPaginationService paginationService, IReadRepository<ProcessGroup> processGroupRepository) : IRequestHandler<GetAllProcessGroupQuery, PaginationResponse<ProcessGroupDto>>
{
    public async Task<PaginationResponse<ProcessGroupDto>> Handle(GetAllProcessGroupQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            // Lấy toàn bộ rồi sắp xếp, cắt trang trong bộ nhớ — xem ToPaginationResponse
            IgnorePagination = true
        };

        var spec = new ProcessGroupsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: processGroupRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        result = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(o => o.Name).ToPaginationResponse(request.PageIndex, request.PageSize, request.IgnorePagination);
        return result;
    }
}
