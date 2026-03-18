using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Commands;
public record DeleteProductUnitPriceListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteProductUnitPriceListCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<DeleteProductUnitPriceListCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();

    public async Task<bool> Handle(DeleteProductUnitPriceListCommand request, CancellationToken cancellationToken)
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
            include: x => x
                .Include(x => x.Outputs).Include(x => x.PlannedMaterialCosts)
                .Include(x => x.PlannedMaintainCosts)
                .Include(x => x.PlannedElectricityCosts),
            disableTracking: true);

        if (itemsToDelete == null || !itemsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (itemsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
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