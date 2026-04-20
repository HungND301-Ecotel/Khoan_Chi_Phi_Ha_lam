using Application.Catalog.MasterData.FixedKeys;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Queries;
public record GetProcessGroupByIdQuery(DefaultIdType Id) : IRequest<ProcessGroupDto>;

public class GetProcessGroupByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProcessGroupByIdQuery, ProcessGroupDto>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<ProcessGroupDto> Handle(GetProcessGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var processGroup = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: q => q.Include(x => x.Code).Include(x => x.FixedKey),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ProcessGroupDto
        {
            Id = processGroup.Id,
            Code = processGroup.Code?.Value ?? string.Empty,
            Name = processGroup.Name,
            Type = FixedKeyCodeMapper.ResolveProcessGroupType(processGroup.FixedKey),
            FixedKeyId = processGroup.FixedKeyId,
            FixedKeyCode = processGroup.FixedKey?.Code,
            FixedKeyName = processGroup.FixedKey?.Name,
        };
    }
}
