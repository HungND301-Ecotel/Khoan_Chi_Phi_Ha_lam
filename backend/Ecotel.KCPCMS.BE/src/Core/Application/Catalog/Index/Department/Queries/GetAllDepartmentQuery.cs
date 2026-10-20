using Application.Catalog.Index.Department.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Department;
using MediatR;

namespace Application.Catalog.Index.Department.Queries;

public record class GetAllDepartmentQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<DepartmentDto>>;

public class GetAllDepartmentQueryHandler(
    IPaginationService paginationService,
    IReadRepository<Domain.Entities.Index.Department> departmentRepository)
    : IRequestHandler<GetAllDepartmentQuery, PaginationResponse<DepartmentDto>>
{
    public async Task<PaginationResponse<DepartmentDto>> Handle(GetAllDepartmentQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            // Lấy toàn bộ rồi sắp xếp, cắt trang trong bộ nhớ — xem ToPaginationResponse
            IgnorePagination = true
        };

        var spec = new DepartmentsByPaginationSpec(filter, request.Search);

        var result = await paginationService.PaginatedListAsync(
            repository: departmentRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result = result.Data.OrderByCodeNatural(d => d.Code).ThenBy(d => d.Name).ToPaginationResponse(request.PageIndex, request.PageSize, request.IgnorePagination);
        return result;
    }
}
