using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Employee;
using Application.Utility; 
using Domain.Entities.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record ChangeEmployeePasswordCommand(ChangeEmployeePasswordDto UpdateModel) : IRequest<bool>;

public class ChangeEmployeePasswordCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ChangeEmployeePasswordCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();
    private readonly IWriteRepository<User> _userRepository = unitOfWork.GetRepository<User>();

    public async Task<bool> Handle(ChangeEmployeePasswordCommand request, CancellationToken cancellationToken)
    {
        var existEmployee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.EmployeeId,
            include: q => q.Include(e => e.User),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (existEmployee.User == null)
        {
            throw new NotFoundException("Không tìm thấy tài khoản của nhân viên này.");
        }

        string currentPasswordHash = Utils.ComputeHash(request.UpdateModel.CurrentPassword);

        if (existEmployee.User.PasswordHash != currentPasswordHash)
        {
            throw new BadRequestException("Mật khẩu hiện tại không đúng.");
        }

        var newHashedPassword = Utils.ComputeHash(request.UpdateModel.NewPassword);
        existEmployee.User.SetPassword(newHashedPassword);

        _userRepository.Update(existEmployee.User);

        await unitOfWork.SaveChangesAsync();
        return true;
    }
}