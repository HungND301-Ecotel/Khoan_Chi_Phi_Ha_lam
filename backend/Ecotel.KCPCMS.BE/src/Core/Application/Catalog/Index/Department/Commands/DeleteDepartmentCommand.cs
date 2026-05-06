using Application.Common.Exceptions;
using Application.Common.Caching;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Department.Commands;

public record DeleteDepartmentCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteDepartmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<DeleteDepartmentCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";

    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Department>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var existDepartment = await _departmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: q => q.Include(d => d.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _departmentRepository.Delete(existDepartment);
            _codeRepository.Delete(existDepartment.Code);
            await unitOfWork.SaveChangesAsync();
            cacheService.InvalidateGroup(CacheSignalKey);
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
