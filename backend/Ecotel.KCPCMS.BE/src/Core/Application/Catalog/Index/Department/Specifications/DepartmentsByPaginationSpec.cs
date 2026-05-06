using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Department;
using Ardalis.Specification;

namespace Application.Catalog.Index.Department.Specifications;

public class DepartmentsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Department, DepartmentDto>
{
    public DepartmentsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(d => d.Code != null && (string.IsNullOrWhiteSpace(searchTerm) ||
                                           d.Name.ToLower().Contains(searchTerm) ||
                                           d.Code.Value.ToLower().Contains(searchTerm)));

        Query
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code.Value,
                Name = d.Name
            });
    }
}
