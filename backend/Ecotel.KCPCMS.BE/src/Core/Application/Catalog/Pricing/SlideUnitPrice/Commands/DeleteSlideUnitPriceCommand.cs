using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.SlideUnitPrice.Commands;

public record DeleteSlideUnitPriceCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteSLideUnitPriceCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<DeleteSlideUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _slideUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "SlideUnitPrice";

    public async Task<bool> Handle(DeleteSlideUnitPriceCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existUnitOfMeasure = await _slideUnitPriceRepository.GetFirstOrDefaultAsync(
                predicate: t => t.Id == request.DeleteId,
                include: t => t
                    .Include(t => t.SlideUnitPriceAssignmentCodes)
                    .ThenInclude(sa => sa.PlannedMaterialCosts),
                disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

            // Get all ProductUnitPriceIds from PlannedMaterialCosts that reference this SlideUnitPrice
            var affectedProductUnitPriceIds = existUnitOfMeasure.SlideUnitPriceAssignmentCodes
                .SelectMany(sa => sa.PlannedMaterialCosts)
                .Select(p => p.ProductUnitPriceId)
                .Distinct()
                .ToList();

            _slideUnitPriceRepository.Delete(existUnitOfMeasure);
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

        if (productUnitPrices.Any())
        {
            _productUnitPriceRepository.Delete(productUnitPrices);
            await unitOfWork.SaveChangesAsync();
        }
    }
}
