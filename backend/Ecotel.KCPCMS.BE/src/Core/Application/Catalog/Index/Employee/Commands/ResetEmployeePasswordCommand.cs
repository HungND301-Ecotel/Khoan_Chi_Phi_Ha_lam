using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Utility; 
using Domain.Entities.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record ResetEmployeePasswordCommand(int EmployeeId) : IRequest<bool>;

public class ResetEmployeePasswordCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ResetEmployeePasswordCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<bool> Handle(ResetEmployeePasswordCommand request, CancellationToken cancellationToken)
    {
        var existEmployee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.EmployeeId,
            include: q => q.Include(e => e.User),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (existEmployee.User == null)
        {
            throw new NotFoundException("Không tìm thấy tài khoản của nhân viên này.");
        }

        existEmployee.User.SetPassword(Utils.ComputeHash(InitialAccountPassword.Read("INITIAL_ACCOUNT_PASSWORD")));

        var refreshTokens = unitOfWork.GetRepository<RefreshToken>();
        var sessions = await refreshTokens.GetAllAsync(
            predicate: token => token.UserId == existEmployee.User.Id,
            disableTracking: false);
        if (sessions.Count > 0)
        {
            refreshTokens.Delete(sessions.ToArray());
        }

        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
