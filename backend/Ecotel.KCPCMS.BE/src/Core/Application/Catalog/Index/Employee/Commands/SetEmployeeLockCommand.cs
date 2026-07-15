using Microsoft.EntityFrameworkCore;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record SetEmployeeLockCommand(int EmployeeId, bool IsLocked) : IRequest<bool>;

public class SetEmployeeLockCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SetEmployeeLockCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<bool> Handle(SetEmployeeLockCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.EmployeeId,
            include: q => q.Include(e => e.User),
            disableTracking: false)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (employee.User == null)
        {
            throw new NotFoundException("Không tìm thấy tài khoản của nhân viên này.");
        }

        if (request.IsLocked)
        {
            employee.User.LockAccount(TimeSpan.FromDays(365 * 100));
        }
        else
        {
            employee.User.UnlockAccount();
        }

        await unitOfWork.SaveChangesAsync();
        return true;
    }
}