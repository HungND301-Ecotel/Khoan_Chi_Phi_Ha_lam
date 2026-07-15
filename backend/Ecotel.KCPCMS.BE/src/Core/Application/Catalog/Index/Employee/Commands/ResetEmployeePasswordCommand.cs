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
    private const string DefaultPassword = "123456";
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

        existEmployee.User.SetPassword(Utils.ComputeHash(DefaultPassword));

        await unitOfWork.SaveChangesAsync();
        return true;
    }
}