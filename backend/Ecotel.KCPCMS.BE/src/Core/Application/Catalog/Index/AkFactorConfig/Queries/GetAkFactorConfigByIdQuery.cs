using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;

namespace Application.Catalog.Index.AkFactorConfig.Queries;

public record GetAkFactorConfigByIdQuery(DefaultIdType Id) : IRequest<AkFactorConfigDto>;

public class GetAkFactorConfigByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAkFactorConfigByIdQuery, AkFactorConfigDto>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();

    public async Task<AkFactorConfigDto> Handle(GetAkFactorConfigByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _akFactorConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: q => q.Include(x => x.ProcessGroup),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new AkFactorConfigDto
        {
            Id = entity.Id,
            ProcessGroupId = entity.ProcessGroupId,
            ProcessGroupCode = entity.ProcessGroup?.Code?.Value ?? string.Empty,
            ProcessGroupName = entity.ProcessGroup?.Name ?? string.Empty,
            MinAkDiff = entity.MinAkDiff,
            MaxAkDiff = entity.MaxAkDiff,
            MinAdjustmentRate = entity.MinAdjustmentRate,
            MaxAdjustmentRate = entity.MaxAdjustmentRate,
            AkDiffDisplay = entity.AkDiffDisplay,
            AdjustmentRateDisplay = entity.AdjustmentRateDisplay,
            Description = entity.Description,
            CreateOn = entity.CreatedOn
        };
    }
}
