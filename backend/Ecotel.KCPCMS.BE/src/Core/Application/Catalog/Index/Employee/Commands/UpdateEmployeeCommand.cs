using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using MediatR;      
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;

public record UpdateEmployeeCommand(int Id, UpdateEmployeeDto UpdateModel) : IRequest<bool>;

public class UpdateEmployeeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateEmployeeCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();


    public async Task<bool> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var existEmployee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        // Chỉ update 2 trường BM cho phép: avatar , password
        existEmployee.UpdateEmployee(
        fullName: request.UpdateModel.FullName.Trim(),
        positionId: request.UpdateModel.PositionId,
        departmentId: request.UpdateModel.DepartmentId,

        avatarUrl: existEmployee.Avatar,

        dob: request.UpdateModel.Dob,
        gender: request.UpdateModel.Gender,
        cccd: request.UpdateModel.Cccd?.Trim() ?? string.Empty,
        province: request.UpdateModel.Province?.Trim() ?? string.Empty,
        district: request.UpdateModel.District?.Trim(),
        ward: request.UpdateModel.Ward?.Trim() ?? string.Empty,
        streetAddress: request.UpdateModel.StreetAddress?.Trim() ?? string.Empty


);

        _employeeRepository.Update(existEmployee);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
