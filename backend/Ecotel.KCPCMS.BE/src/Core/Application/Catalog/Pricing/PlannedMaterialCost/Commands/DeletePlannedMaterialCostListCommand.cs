using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedMaterialCost.Commands;
public record DeletePlannedMaterialCostListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeletePlannedMaterialCostListCommandHandler(
    ICacheService cacheService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePlannedMaterialCostListCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();

    public async Task<bool> Handle(DeletePlannedMaterialCostListCommand request, CancellationToken cancellationToken)
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

        var itemsToDelete = await _repository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (itemsToDelete == null || !itemsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (itemsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.PlannedMaterialCostNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _repository.Delete(itemsToDelete);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(CacheSignalKey);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}