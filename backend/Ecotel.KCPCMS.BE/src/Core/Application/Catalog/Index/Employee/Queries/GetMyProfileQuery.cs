using Microsoft.EntityFrameworkCore;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Queries;

public record GetMyProfileQuery(int EmployeeId) : IRequest<EmployeeDto>;

public class GetMyProfileQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyProfileQuery, EmployeeDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<EmployeeDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.EmployeeId,
            include: q => q
                .Include(e => e.Position)
                .Include(e => e.Department)
                .Include(e => e.User),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new EmployeeDto
        {
            Id = employee.Id,
            FullName = employee.FullName,
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name,
            UserId = employee.UserId,
            UserName = employee.User?.UserName,
            Cccd = employee.Cccd,
            Dob = employee.Dob ?? default,
            Genre = employee.Gender ?? true,
            IsActive = employee.User.LockoutEnabled,    
            Avatar = employee.Avatar,
            Email = employee.User?.Email,
            PhoneNumber = employee.User?.PhoneNumber
        };
    }
}
