using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Position;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Position.Queries;

public record GetPositionByIdQuery(int Id) : IRequest<PositionDto>;

public class GetPositionByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPositionByIdQuery, PositionDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository = unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<PositionDto> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _positionRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new PositionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            IsActive = entity.IsActive
        };
    }
}
