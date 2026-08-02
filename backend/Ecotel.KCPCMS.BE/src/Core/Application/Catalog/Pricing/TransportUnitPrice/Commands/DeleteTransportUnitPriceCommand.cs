using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Commands;

public record DeleteTransportUnitPriceCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteTransportUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTransportUnitPriceCommand, bool>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _transportUnitPriceRepository = unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<bool> Handle(DeleteTransportUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var existTransportUnitPrice = await _transportUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.TransportUnitPriceNotFound);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            _transportUnitPriceRepository.Delete(existTransportUnitPrice);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        return true;
    }
}