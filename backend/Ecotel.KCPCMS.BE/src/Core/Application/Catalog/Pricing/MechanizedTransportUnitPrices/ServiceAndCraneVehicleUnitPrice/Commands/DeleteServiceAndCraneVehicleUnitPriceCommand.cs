using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ServiceAndCraneVehicleUnitPrice.Commands;

public record DeleteServiceAndCraneVehicleUnitPriceCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteServiceAndCraneVehicleUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteServiceAndCraneVehicleUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice>();

    public async Task<bool> Handle(DeleteServiceAndCraneVehicleUnitPriceCommand request, CancellationToken cancellationToken)
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

public record DeleteServiceAndCraneVehicleUnitPriceListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteServiceAndCraneVehicleUnitPriceListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteServiceAndCraneVehicleUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ServiceAndCraneVehicleUnitPrice>();

    public async Task<bool> Handle(DeleteServiceAndCraneVehicleUnitPriceListCommand request, CancellationToken cancellationToken)
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