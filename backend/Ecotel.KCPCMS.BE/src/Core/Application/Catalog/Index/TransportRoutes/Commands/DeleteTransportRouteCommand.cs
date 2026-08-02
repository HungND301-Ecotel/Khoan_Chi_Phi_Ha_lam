using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.TransportRoutes.Commands;

public record DeleteTransportRouteCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteTransportRouteCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTransportRouteCommand, bool>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();

    public async Task<bool> Handle(DeleteTransportRouteCommand request, CancellationToken cancellationToken)
    {
        var existTransportRoute = await _transportRouteRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: query => query.Include(t => t.TransportUnitPrices),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (existTransportRoute.TransportUnitPrices.Any())
        {
            throw new ConflictException(CustomResponseMessage.TransportRouteInUse);
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            _transportRouteRepository.Delete(existTransportRoute);
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