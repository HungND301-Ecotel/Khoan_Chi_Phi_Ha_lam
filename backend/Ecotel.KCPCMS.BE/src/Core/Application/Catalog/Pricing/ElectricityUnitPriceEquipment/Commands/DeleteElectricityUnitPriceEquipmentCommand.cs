using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;

public record DeleteElectricityUnitPriceEquipmentCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteElectricityUnitPriceEquipmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<DeleteElectricityUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _electricityUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "ElectricityUnitPriceEquipment";

    public async Task<bool> Handle(DeleteElectricityUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var unitPriceEquipment = await _electricityUnitPriceRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.DeleteId,
                include: t => t
                    .Include(t => t.PlannedElectricityCostAdjustmentFactors)
                    .ThenInclude(peca => peca.PlannedElectricityCost),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            // Get all ProductUnitPriceIds from PlannedElectricityCosts that reference this ElectricityUnitPrice
            var affectedProductUnitPriceIds = unitPriceEquipment.PlannedElectricityCostAdjustmentFactors
                .Where(peca => peca.PlannedElectricityCost != null)
                .Select(peca => peca.PlannedElectricityCost!.ProductUnitPriceId)
                .Distinct()
                .ToList();

            _electricityUnitPriceRepository.Delete(unitPriceEquipment);
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
