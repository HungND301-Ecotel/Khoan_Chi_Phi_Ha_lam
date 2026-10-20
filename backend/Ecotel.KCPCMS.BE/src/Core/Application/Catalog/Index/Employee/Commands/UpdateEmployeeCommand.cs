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
    private readonly IWriteRepository<Domain.Entities.Identity.User> _userRepository = unitOfWork.GetRepository<Domain.Entities.Identity.User>();


    public async Task<bool> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var existEmployee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (string.IsNullOrWhiteSpace(request.UpdateModel.Code))
        {
            throw new BadRequestException("Mã nhân viên không được để trống.");
        }

        var code = request.UpdateModel.Code.Trim().ToUpper();
        if (await _employeeRepository.AnyAsync(e => e.Code == code && e.Id != request.Id))
        {
            throw new ConflictException("Mã nhân viên đã tồn tại.");
        }

        var existUser = await _userRepository.GetFirstOrDefaultAsync(
            predicate: u => u.Id == existEmployee.UserId,
            disableTracking: true);

        if (existUser != null)
        {
            existUser.Update(request.UpdateModel.PhoneNumber, request.UpdateModel.Email);
            _userRepository.Update(existUser);
        }

        // Chỉ update 2 trường BM cho phép: avatar , password
        existEmployee.UpdateEmployee(
        fullName: request.UpdateModel.FullName.Trim(),
        positionId: request.UpdateModel.PositionId,
        departmentId: request.UpdateModel.DepartmentId,
        avatarUrl: existEmployee.Avatar,
        dob: request.UpdateModel.Dob,
        gender: request.UpdateModel.Gender,
        cccd: request.UpdateModel.Cccd?.Trim() ?? string.Empty,
        code: code,
        note: request.UpdateModel.Note);

        _employeeRepository.Update(existEmployee);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
