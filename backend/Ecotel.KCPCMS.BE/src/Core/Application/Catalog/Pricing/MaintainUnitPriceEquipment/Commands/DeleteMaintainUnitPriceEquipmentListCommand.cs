// File: Application/Catalog/Pricing/MaintainUnitPriceEquipment/Commands/DeleteMaintainUnitPriceEquipmentListCommand.cs
using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;

public record DeleteMaintainUnitPriceEquipmentListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteMaintainUnitPriceEquipmentListCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<DeleteMaintainUnitPriceEquipmentListCommand, bool>
{
    private readonly IWriteRepository<MaintainUnitPrice> _repository = unitOfWork.GetRepository<MaintainUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(DeleteMaintainUnitPriceEquipmentListCommand request, CancellationToken cancellationToken)
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
            include: q => q
                .Include(m => m.MaintainUnitPriceEquipments)
                .Include(m => m.PlannedMaintainCostAdjustmentFactors)
                .ThenInclude(pmca => pmca.PlannedMaintainCost),
            disableTracking: true);

        if (itemsToDelete == null || !itemsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (itemsToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.MaintainUnitPriceNotFound);
        }

        // Get all affected ProductUnitPriceIds
        var affectedProductUnitPriceIds = itemsToDelete
            .SelectMany(m => m.PlannedMaintainCostAdjustmentFactors)
            .Where(pmca => pmca.PlannedMaintainCost != null)
            .Select(pmca => pmca.PlannedMaintainCost!.ProductUnitPriceId)
            .Distinct()
            .ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _repository.Delete(itemsToDelete);
            await unitOfWork.SaveChangesAsync();

            // Check and delete ProductUnitPrice if they have no remaining PlannedCosts
            await DeleteOrphanProductUnitPrices(affectedProductUnitPriceIds, cancellationToken);

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

    private async Task DeleteOrphanProductUnitPrices(List<Guid> productUnitPriceIds, CancellationToken cancellationToken)
    {
        if (!productUnitPriceIds.Any())
        {
            return;
        }

        var productUnitPrices = await _productUnitPriceRepository.GetAllAsync(
            predicate: p => productUnitPriceIds.Contains(p.Id),
            include: p => p
                .Include(p => p.Outputs),
            disableTracking: true);

        if (productUnitPrices.Any())
        {
            _productUnitPriceRepository.Delete(productUnitPrices);
            await unitOfWork.SaveChangesAsync();
        }
    }
}