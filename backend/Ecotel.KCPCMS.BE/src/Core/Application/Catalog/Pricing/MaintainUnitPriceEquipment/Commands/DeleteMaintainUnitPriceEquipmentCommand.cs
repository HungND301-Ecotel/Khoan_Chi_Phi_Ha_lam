using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;

public record DeleteMaintainUnitPriceEquipmentCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteMaintainUnitPriceEquipmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<DeleteMaintainUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPrice> _maintainUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "MaintainUnitPriceEquipment";

    public async Task<bool> Handle(DeleteMaintainUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existUnitOfMeasure = await _maintainUnitPriceRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.DeleteId,
                include: t => t
                    .Include(t => t.MaintainUnitPriceEquipments)
                    .Include(t => t.PlannedMaintainCostAdjustmentFactors)
                    .ThenInclude(pmca => pmca.PlannedMaintainCost),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            // Get all ProductUnitPriceIds from PlannedMaintainCosts that reference this MaintainUnitPrice
            var affectedProductUnitPriceIds = existUnitOfMeasure.PlannedMaintainCostAdjustmentFactors
                .Where(pmca => pmca.PlannedMaintainCost != null)
                .Select(pmca => pmca.PlannedMaintainCost!.ProductUnitPriceId)
                .Distinct()
                .ToList();

            _maintainUnitPriceRepository.Delete(existUnitOfMeasure);
            await unitOfWork.SaveChangesAsync();

            // Check and delete ProductUnitPrice if they have no remaining PlannedCosts
            await DeleteOrphanProductUnitPrices(affectedProductUnitPriceIds, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);
            cacheService.InvalidateGroup(CacheSignalKey);
            cacheService.InvalidateGroup(ModuleCacheSignalKey);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        return true;
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
