using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportRoute;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.TransportRoutes.Commands;

public record CreateTransportRouteCommand(CreateTransportRouteDto CreateModel) : IRequest<bool>;

public class CreateTransportRouteCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateTransportRouteCommand, bool>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();

    public async Task<bool> Handle(CreateTransportRouteCommand request, CancellationToken cancellationToken)
    {
        var checkCodeExisted = await _transportRouteRepository.AnyAsync(
            t => t.Code != null && t.Code.Value.ToUpper() == request.CreateModel.Code.Trim().ToUpper());
        if (checkCodeExisted)
        {
            throw new ConflictException(CustomResponseMessage.TransportRouteCodeAlreadyExists);
        }

        var newTransportRoute = TransportRoute.Create(
            request.CreateModel.Code,
            request.CreateModel.Name,
            request.CreateModel.Note,
            request.CreateModel.IsSpecialLowVolume);

        await _transportRouteRepository.InsertAsync(newTransportRoute, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}