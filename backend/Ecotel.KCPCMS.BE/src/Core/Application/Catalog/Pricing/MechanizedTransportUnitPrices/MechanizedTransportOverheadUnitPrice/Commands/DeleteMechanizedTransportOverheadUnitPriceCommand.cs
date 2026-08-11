using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Commands;

public record DeleteMechanizedTransportOverheadUnitPriceCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteMechanizedTransportOverheadUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteMechanizedTransportOverheadUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();

    public async Task<bool> Handle(DeleteMechanizedTransportOverheadUnitPriceCommand request, CancellationToken cancellationToken)
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

public record DeleteMechanizedTransportOverheadUnitPriceListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteMechanizedTransportOverheadUnitPriceListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteMechanizedTransportOverheadUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();

    public async Task<bool> Handle(DeleteMechanizedTransportOverheadUnitPriceListCommand request, CancellationToken cancellationToken)
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
