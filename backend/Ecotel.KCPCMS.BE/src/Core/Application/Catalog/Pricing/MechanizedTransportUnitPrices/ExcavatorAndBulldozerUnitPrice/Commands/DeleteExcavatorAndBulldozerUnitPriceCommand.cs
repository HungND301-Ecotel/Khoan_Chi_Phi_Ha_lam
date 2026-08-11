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

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Commands;

public record DeleteExcavatorAndBulldozerUnitPriceCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteExcavatorAndBulldozerUnitPriceCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteExcavatorAndBulldozerUnitPriceCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();

    public async Task<bool> Handle(DeleteExcavatorAndBulldozerUnitPriceCommand request, CancellationToken cancellationToken)
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

public record DeleteExcavatorAndBulldozerUnitPriceListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteExcavatorAndBulldozerUnitPriceListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteExcavatorAndBulldozerUnitPriceListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();

    public async Task<bool> Handle(DeleteExcavatorAndBulldozerUnitPriceListCommand request, CancellationToken cancellationToken)
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
