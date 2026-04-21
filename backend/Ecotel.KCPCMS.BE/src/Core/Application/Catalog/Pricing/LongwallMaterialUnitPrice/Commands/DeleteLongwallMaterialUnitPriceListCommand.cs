using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;

public record DeleteLongwallMaterialUnitPriceListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteLongwallMaterialUnitPriceListCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<DeleteLongwallMaterialUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> _materialUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    private const string ProductUnitPriceCacheSignalKey = "ProductUnitPrice";
    private const string LongwallMaterialUnitPriceCacheSignalKey = "LongwallMaterialUnitPrice";

    public async Task<bool> Handle(DeleteLongwallMaterialUnitPriceListCommand request, CancellationToken cancellationToken)
    {
        if (!request.DeleteIds.Any())
        {
            throw new BadRequestException("No items selected for deletion");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var materialsToDelete = await _materialUnitPriceRepository.GetAllAsync(
                predicate: t => request.DeleteIds.Contains(t.Id),
                include: t => t
                    .Include(t => t.Code)
                    .Include(t => t.PlannedMaterialCosts)
                    .Include(m => m.MaterialUnitPriceAssignmentCodes),
                disableTracking: true);

            if (materialsToDelete.Count == 0)
            {
                throw new NotFoundException(CustomResponseMessage.EntityNotFound);
            }

            var affectedProductUnitPriceIds = materialsToDelete
                .SelectMany(m => m.PlannedMaterialCosts.Select(p => p.ProductUnitPriceId))
                .Distinct()
                .ToList();

            foreach (var material in materialsToDelete)
            {
                _materialUnitPriceRepository.Delete(material);
                if (material.Code != null)
                {
                    _codeRepository.Delete(material.Code);
                }
            }

            await unitOfWork.SaveChangesAsync();

            await DeleteOrphanProductUnitPrices(affectedProductUnitPriceIds, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(ProductUnitPriceCacheSignalKey);
            cacheService.InvalidateGroup(LongwallMaterialUnitPriceCacheSignalKey);
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
            include: p => p.Include(p => p.Outputs),
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
