using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Commands;

public record DeleteLowValuePerishableSupplyUnitPriceListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteLowValuePerishableSupplyUnitPriceListCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<DeleteLowValuePerishableSupplyUnitPriceListCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "LowValuePerishableSupplyUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();

    public async Task<bool> Handle(DeleteLowValuePerishableSupplyUnitPriceListCommand request, CancellationToken cancellationToken)
    {
        List<DefaultIdType> distinctIds = request.DeleteIds.Distinct().ToList();
        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var items = await _repository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (items.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceNotFound);
        }

        _repository.Delete(items);
        await unitOfWork.SaveChangesAsync();
        cacheService.InvalidateGroup(CacheSignalKey);
        cacheService.InvalidateGroup(ModuleCacheSignalKey);
        return true;
    }
}