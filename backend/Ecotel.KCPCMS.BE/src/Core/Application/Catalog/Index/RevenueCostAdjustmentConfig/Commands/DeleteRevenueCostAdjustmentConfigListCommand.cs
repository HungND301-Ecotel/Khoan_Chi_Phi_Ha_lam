using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Commands;

public record DeleteRevenueCostAdjustmentConfigListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteRevenueCostAdjustmentConfigListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteRevenueCostAdjustmentConfigListCommand, bool>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<bool> Handle(DeleteRevenueCostAdjustmentConfigListCommand request, CancellationToken cancellationToken)
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

        var entitiesToDelete = await _revenueCostAdjustmentConfigRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (entitiesToDelete == null || !entitiesToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (entitiesToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.EntityNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _revenueCostAdjustmentConfigRepository.Delete(entitiesToDelete);
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
