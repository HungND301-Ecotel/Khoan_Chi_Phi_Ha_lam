// File: Application/Catalog/Metrics/Commands/DeleteMetricListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Metrics.Commands;

public record DeleteMetricListCommand<TEntity>(IList<DefaultIdType> DeleteIds) : IRequest<bool>
    where TEntity : class;

public class DeleteMetricListCommandHandler<TEntity>(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMetricListCommand<TEntity>, bool>
    where TEntity : class
{
    private readonly IWriteRepository<TEntity> _repository = unitOfWork.GetRepository<TEntity>();

    public async Task<bool> Handle(DeleteMetricListCommand<TEntity> request, CancellationToken cancellationToken)
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

        var entitiesToDelete = await _repository.GetAllAsync(
            predicate: e => distinctIds.Contains(EF.Property<DefaultIdType>(e, "Id")),
            disableTracking: true);

        if (entitiesToDelete == null || !entitiesToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (entitiesToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.PartNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _repository.Delete(entitiesToDelete);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}