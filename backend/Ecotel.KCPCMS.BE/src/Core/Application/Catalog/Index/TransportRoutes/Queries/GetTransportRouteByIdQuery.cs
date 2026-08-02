using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportRoute;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.TransportRoutes.Queries;

public record GetTransportRouteByIdQuery(DefaultIdType Id) : IRequest<TransportRouteDto>;

public class GetTransportRouteByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetTransportRouteByIdQuery, TransportRouteDto>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();

    public async Task<TransportRouteDto> Handle(GetTransportRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var transportRoute = await _transportRouteRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: query => query.Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new TransportRouteDto
        {
            Id = transportRoute.Id,
            Code = transportRoute.Code?.Value ?? string.Empty,
            Name = transportRoute.Name,
            Note = transportRoute.Note,
            ProductionProcessId = transportRoute.ProductionProcessId,
            IsSpecialLowVolume = transportRoute.IsSpecialLowVolume
        };
    }
}