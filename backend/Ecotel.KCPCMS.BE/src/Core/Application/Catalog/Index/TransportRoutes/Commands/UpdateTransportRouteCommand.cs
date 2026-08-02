using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportRoute;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.TransportRoutes.Commands;

public record UpdateTransportRouteCommand(TransportRouteDto UpdateModel) : IRequest<bool>;

public class UpdateTransportRouteCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateTransportRouteCommand, bool>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();

    public async Task<bool> Handle(UpdateTransportRouteCommand request, CancellationToken cancellationToken)
    {
        var checkCodeExisted = await _transportRouteRepository.AnyAsync(
            t => t.Code != null
                && t.Code.Value.ToUpper() == request.UpdateModel.Code.Trim().ToUpper()
                && t.Id != request.UpdateModel.Id);
        if (checkCodeExisted)
        {
            throw new ConflictException(CustomResponseMessage.TransportRouteCodeAlreadyExists);
        }

        var existTransportRoute = await _transportRouteRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: query => query.Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existTransportRoute.Update(
            request.UpdateModel.Code.Trim(),
            request.UpdateModel.Name.Trim(),
            request.UpdateModel.Note?.Trim(),
            request.UpdateModel.ProductionProcessId,
            request.UpdateModel.IsSpecialLowVolume);

        _transportRouteRepository.Update(existTransportRoute);
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitAsync();
        return true;
    }
}