using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Employee.Commands;

public record DeleteEmployeeListCommand(List<int> Ids) : IRequest<bool>;

public class DeleteEmployeeListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteEmployeeListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Employee> _employeeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Employee>();

    public async Task<bool> Handle(DeleteEmployeeListCommand request, CancellationToken cancellationToken)
    {
        var entities = await _employeeRepository.GetAllAsync(
            predicate: e => request.Ids.Contains(e.Id),
            include: q => q.Include(e => e.User),
            disableTracking: true);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            foreach (var employee in entities)
            {
                employee.User?.DeleteUser();
            }

            _employeeRepository.Delete(entities);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
