using Application.Catalog.Index.AssignmentCodes.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.AssignmentCode;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.AssignmentCodes.Queries;

public record GetAllAssignmentCodeQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<AssignmentCodeDto>>;

public class GetAllAssignmentCodeQueryHandler(IPaginationService paginationService, IReadRepository<AssignmentCode> assignemntCodeRepository) : IRequestHandler<GetAllAssignmentCodeQuery, PaginationResponse<AssignmentCodeDto>>
{
    public async Task<PaginationResponse<AssignmentCodeDto>> Handle(GetAllAssignmentCodeQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            // Lấy toàn bộ rồi sắp xếp, cắt trang trong bộ nhớ — xem ToPaginationResponse
            IgnorePagination = true
        };

        var spec = new AssignmentCodesByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: assignemntCodeRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
        result = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(d => d.Name).ToPaginationResponse(request.PageIndex, request.PageSize, request.IgnorePagination);
        return result;
    }
}

