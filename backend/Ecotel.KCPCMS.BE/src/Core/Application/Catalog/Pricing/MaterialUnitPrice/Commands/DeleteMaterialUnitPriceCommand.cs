using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Commands;

public record DeleteMaterialUnitPriceCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteMaterialUnitPriceCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<DeleteMaterialUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "MaterialUnitPrice";

    public async Task<bool> Handle(DeleteMaterialUnitPriceCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existUnitOfMeasure = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.DeleteId,
                include: t => t.Include(t => t.Code).Include(t => t.PlannedMaterialCosts),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            // Get all ProductUnitPriceIds that reference this MaterialUnitPrice
            var affectedProductUnitPriceIds = existUnitOfMeasure.PlannedMaterialCosts
                .Select(p => p.ProductUnitPriceId)
                .Distinct()
                .ToList();

            _materialUnitPriceRepository.Delete(existUnitOfMeasure);
            _codeRepository.Delete(existUnitOfMeasure.Code);
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

        var orphanProducts = productUnitPrices
            .Where(p => !p.PlannedMaterialCosts.Any() &&
                       !p.PlannedMaintainCosts.Any() &&
                       !p.PlannedElectricityCosts.Any() &&
                       !p.Outputs.Any())
            .ToList();

        if (orphanProducts.Any())
        {
            _productUnitPriceRepository.Delete(orphanProducts);
            await unitOfWork.SaveChangesAsync();
        }
    }
}
