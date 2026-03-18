using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Queries;
public record GetProcessGroupByIdQuery(DefaultIdType Id) : IRequest<ProcessGroupDto>;

public class GetProcessGroupByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProcessGroupByIdQuery, ProcessGroupDto>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<ProcessGroupDto> Handle(GetProcessGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var unitOfMeasure = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return unitOfMeasure.Adapt<ProcessGroupDto>();
    }
}
