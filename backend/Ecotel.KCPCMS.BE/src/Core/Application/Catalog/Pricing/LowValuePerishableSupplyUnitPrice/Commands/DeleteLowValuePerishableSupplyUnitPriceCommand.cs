using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Commands;

public record DeleteLowValuePerishableSupplyUnitPriceCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteLowValuePerishableSupplyUnitPriceCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<DeleteLowValuePerishableSupplyUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "LowValuePerishableSupplyUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();

    public async Task<bool> Handle(DeleteLowValuePerishableSupplyUnitPriceCommand request, CancellationToken cancellationToken)
    {
        Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceNotFound);

        _repository.Delete(entity);
        await unitOfWork.SaveChangesAsync();
        cacheService.InvalidateGroup(CacheSignalKey);
        cacheService.InvalidateGroup(ModuleCacheSignalKey);
        return true;
    }
}