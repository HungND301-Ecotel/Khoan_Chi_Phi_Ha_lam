using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.TransportRoutes.Commands;

public record DeleteTransportRouteListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteTransportRouteListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTransportRouteListCommand, bool>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();

    public async Task<bool> Handle(DeleteTransportRouteListCommand request, CancellationToken cancellationToken)
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

        var transportRoutesToDelete = await _transportRouteRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: query => query.Include(t => t.TransportUnitPrices),
            disableTracking: true);

        if (transportRoutesToDelete == null || !transportRoutesToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (transportRoutesToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.TransportRouteNotFound);
        }

        if (transportRoutesToDelete.Any(t => t.TransportUnitPrices.Any()))
        {
            throw new ConflictException(CustomResponseMessage.TransportRouteInUse);
        }

        await unitOfWork.BeginTransactionAsync();

        try
        {
            _transportRouteRepository.Delete(transportRoutesToDelete);
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