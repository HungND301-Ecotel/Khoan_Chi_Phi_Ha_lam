using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Interfaces.Services;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record ResendEmployeeVerificationCommand(int EmployeeId) : IRequest<bool>;

public class ResendEmployeeVerificationCommandHandler(IUnitOfWork unitOfWork, IUserService userService)
    : IRequestHandler<ResendEmployeeVerificationCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<bool> Handle(ResendEmployeeVerificationCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.EmployeeId,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await userService.ResendVerificationEmail(employee.UserId);
        return true;
    }
}