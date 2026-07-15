using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Employee;
using Ardalis.Specification;

namespace Application.Catalog.Index.Employee.Specifications;

public class EmployeesByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Employee, EmployeeDto>
{
    public EmployeesByPaginationSpec(PaginationFilter filter, string? search,Guid? departmentId) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Where(e => string.IsNullOrWhiteSpace(searchTerm) ||
                        e.FullName.ToLower().Contains(searchTerm) ||
                        (e.User != null && e.User.UserName.ToLower().Contains(searchTerm)))
            .Where(e => departmentId == null || e.DepartmentId == departmentId)
            .Include(e => e.Position)
            .Include(e => e.Department)
            .Include(e => e.User);

        Query
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                PositionId = e.PositionId,
                PositionName = e.Position != null ? e.Position.Name : null,
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                UserId = e.UserId,
                UserName = e.User != null ? e.User.UserName : null,
                Cccd = e.Cccd != null ? e.Cccd : null,
                Avatar = e.Avatar != null ? e.Avatar : null,
                Email = e.User != null ? e.User.Email : null,
                PhoneNumber = e.User != null ? e.User.PhoneNumber : null,
                Dob = e.Dob.HasValue ? e.Dob.Value : DateOnly.MinValue,
                Genre = e.Gender ?? true,
                IsActive = !(e.User.LockoutEnabled && e.User.LockoutEnd != null && e.User.LockoutEnd > DateTimeOffset.UtcNow)
            });
    }
}

