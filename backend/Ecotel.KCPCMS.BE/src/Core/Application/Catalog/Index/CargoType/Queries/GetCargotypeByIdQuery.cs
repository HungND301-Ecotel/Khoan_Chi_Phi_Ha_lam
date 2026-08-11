using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CargoType;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.CargoType.Queries;

public record GetCargoTypeByIdQuery(Guid Id) : IRequest<CargoTypeDto>;

public class GetCargoTypeByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetCargoTypeByIdQuery, CargoTypeDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _cargoTypeRepository = unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<CargoTypeDto> Handle(GetCargoTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _cargoTypeRepository.GetFirstOrDefaultAsync(
            predicate: c => c.Id == request.Id,
            include: c => c.Include(x => x.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new CargoTypeDto
        {
            Id = entity.Id,
            Code = entity.Code.Value,
            Name = entity.Name,
            Note = entity.Note
        };
    }
}