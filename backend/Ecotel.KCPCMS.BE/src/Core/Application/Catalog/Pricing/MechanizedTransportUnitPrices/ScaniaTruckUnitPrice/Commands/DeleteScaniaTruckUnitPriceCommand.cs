using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ScaniaTruckUnitPrice.Commands;

public record DeleteScaniaTruckUnitPriceCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteScaniaTruckUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteScaniaTruckUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();

    public async Task<bool> Handle(DeleteScaniaTruckUnitPriceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.DeleteId,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Delete(entity);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public record DeleteScaniaTruckUnitPriceListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteScaniaTruckUnitPriceListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteScaniaTruckUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ScaniaTruckUnitPrice>();

    public async Task<bool> Handle(DeleteScaniaTruckUnitPriceListCommand request, CancellationToken cancellationToken)
    {
        var entities = await _repository.GetAllAsync(
            predicate: x => request.DeleteIds.Contains(x.Id),
            disableTracking: true);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Delete(entities);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}