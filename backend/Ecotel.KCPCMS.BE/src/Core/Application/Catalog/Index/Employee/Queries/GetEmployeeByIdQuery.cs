using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;


namespace Application.Catalog.Index.Employee.Queries;

public record GetEmployeeByIdQuery(int Id) : IRequest<EmployeeDto>;

public class GetEmployeeByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var existEmployee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id,
            include: q => q
                .Include(e => e.Position)
                .Include(e => e.Department)
                .Include(e => e.User),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new EmployeeDto
        {
            Id = existEmployee.Id,
            FullName = existEmployee.FullName,
            PositionId = existEmployee.PositionId,
            PositionName = existEmployee.Position?.Name,
            DepartmentId = existEmployee.DepartmentId,
            DepartmentName = existEmployee.Department?.Name,
            UserId = existEmployee.UserId,
            UserName = existEmployee.User?.UserName,
            Cccd= existEmployee.Cccd,
            Province = existEmployee.Province,
            Avatar = existEmployee.Avatar,
            Email = existEmployee.User?.Email,
            PhoneNumber = existEmployee.User?.PhoneNumber
        };
    }
}
