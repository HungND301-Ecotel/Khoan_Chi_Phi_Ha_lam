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
            IgnorePagination = request.IgnorePagination
        };

        var spec = new AssignmentCodesByPaginationSpec(filter, request.Search);

        return await paginationService.PaginatedListAsync(
            repository: assignemntCodeRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);
    }
}

