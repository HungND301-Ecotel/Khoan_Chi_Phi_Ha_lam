using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Domain.Entities.Index;
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
        var processGroup = await _processGroupRepository.GetAll()
            .Include(t => t.Code)
            .Include(t => t.FixedKey)
            .Where(t => t.Id == request.Id)
            .Select(t => new ProcessGroupDto
            {
                Id = t.Id,
                FixedKeyId = t.FixedKeyId,
                Code = t.FixedKey != null ? t.FixedKey.Key : t.Code != null ? t.Code.Value : string.Empty,
                Type = t.FixedKey != null ? t.FixedKey.Type : t.Type,
                Name = t.Name,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return processGroup;
    }
}
