using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.SlideUnitPrice.Commands;

public record DeleteSlideUnitPriceListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteSlideUnitPriceListCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<DeleteSlideUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(DeleteSlideUnitPriceListCommand request, CancellationToken cancellationToken)
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
                .Include(s => s.SlideUnitPriceAssignmentCodes)
                .ThenInclude(sa => sa.PlannedMaterialCosts)
                .Include(s => s.Code),
            disableTracking: true);

        if (itemsToDelete == null || !itemsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (itemsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.SlideUnitPriceNotFound);
        }

        // Get all affected ProductUnitPriceIds
        var affectedProductUnitPriceIds = itemsToDelete
            .SelectMany(s => s.SlideUnitPriceAssignmentCodes)
            .SelectMany(sa => sa.PlannedMaterialCosts)
            .Select(p => p.ProductUnitPriceId)
            .Distinct()
            .ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _repository.Delete(itemsToDelete);
            _codeRepository.Delete(itemsToDelete.Select(i => i.Code));
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