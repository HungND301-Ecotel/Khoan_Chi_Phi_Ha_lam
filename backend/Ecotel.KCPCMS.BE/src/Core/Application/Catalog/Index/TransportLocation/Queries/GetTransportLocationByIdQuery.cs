using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportLocation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.TransportLocation.Queries;

public record GetTransportLocationByIdQuery(Guid Id) : IRequest<TransportLocationDto>;

public class GetTransportLocationByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetTransportLocationByIdQuery, TransportLocationDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.TransportLocation> _repository = unitOfWork.GetRepository<Domain.Entities.Index.TransportLocation>();

    public async Task<TransportLocationDto> Handle(GetTransportLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            include: x => x.Include(t => t.Code),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new TransportLocationDto
        {
            Id = entity.Id,
            Code = entity.Code.Value,
            Name = entity.Name,
            Note = entity.Note,
            LocationType = entity.LocationType
        };
    }
}