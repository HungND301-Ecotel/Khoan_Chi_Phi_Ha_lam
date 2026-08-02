using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Commands;

public record DeleteTransportUnitPriceListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteTransportUnitPriceListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTransportUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _transportUnitPriceRepository = unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<bool> Handle(DeleteTransportUnitPriceListCommand request, CancellationToken cancellationToken)
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

        var toDelete = await _transportUnitPriceRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (toDelete == null || !toDelete.Any() || toDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);
        }

        await unitOfWork.BeginTransactionAsync();

        try
        {
            _transportUnitPriceRepository.Delete(toDelete);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}