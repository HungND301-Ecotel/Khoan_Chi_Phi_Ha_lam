using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Catalog.Index.Employee.Specifications;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.Employee;
using MediatR;

namespace Application.Catalog.Index.Employee.Queries;

public record GetAllEmployeeQuery(int PageIndex, int PageSize, string? Search,Guid? DepartmentId, bool IgnorePagination) : IRequest<PaginationResponse<EmployeeDto>>;

public class GetAllEmployeeQueryHandler(
    IPaginationService paginationService,
    IReadRepository<Domain.Entities.Index.Employee> employeeRepository)
    : IRequestHandler<GetAllEmployeeQuery, PaginationResponse<EmployeeDto>>
{
    public async Task<PaginationResponse<EmployeeDto>> Handle(GetAllEmployeeQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new EmployeesByPaginationSpec(filter, request.Search,request.DepartmentId);

        var result = await paginationService.PaginatedListAsync(
            repository: employeeRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        result.Data = result.Data.OrderBy(e => e.FullName).ToList();
        return result;
    }
}
