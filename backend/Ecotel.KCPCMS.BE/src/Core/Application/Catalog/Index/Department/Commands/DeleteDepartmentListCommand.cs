using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Department.Commands;

public record DeleteDepartmentListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteDepartmentListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteDepartmentListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Department>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteDepartmentListCommand request, CancellationToken cancellationToken)
    {
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var departmentsToDelete = await _departmentRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: q => q.Include(d => d.Code),
            disableTracking: true);

        if (departmentsToDelete == null || !departmentsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (departmentsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var codes = departmentsToDelete.Select(d => d.Code);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _departmentRepository.Delete(departmentsToDelete);
            _codeRepository.Delete(codes);
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
