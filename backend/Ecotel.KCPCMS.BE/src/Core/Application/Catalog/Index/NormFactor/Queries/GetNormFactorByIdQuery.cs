using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.NormFactor;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record GetNormFactorByIdQuery(DefaultIdType Id) : IRequest<NormFactorDto>;

public class GetNormFactorByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetNormFactorByIdQuery, NormFactorDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.NormFactor> _normFactorRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.NormFactor>();

    public async Task<NormFactorDto> Handle(GetNormFactorByIdQuery request,
        CancellationToken cancellationToken)
    {
        var normFactor = await _normFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: p => p.Include(p => p.ProductionProcess).ThenInclude(pp => pp.Code)
            .Include(p => p.ProductionProcess).ThenInclude(pp => pp.ProcessGroup).ThenInclude(pg => pg.Code)
            .Include(p => p.Hardness)
            .Include(p => p.StoneClampRatio)
            .Include(p => p.Hardness)
            .Include(p => p.NormFactorAssignmentCodes),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var dto = normFactor.Adapt<NormFactorDto>();
        dto.AffectAssignmentCodeIds = normFactor.NormFactorAssignmentCodes.Select(n => n.AssignmentCodeId).ToList();
        dto.ProductionProcessCode = normFactor.ProductionProcess?.Code?.Value ?? string.Empty;
        dto.ProductionProcessName = normFactor.ProductionProcess?.Name ?? string.Empty;
        dto.HardnessName = normFactor.Hardness?.Value ?? string.Empty;
        dto.StoneClampRatioName = normFactor.StoneClampRatio?.Value ?? string.Empty;
        dto.TargetHardnessName = normFactor.TargetHardness != null ? normFactor.TargetHardness.Value : string.Empty;
        dto.ProcessGroupId = normFactor.ProductionProcess!.ProcessGroupId;
        dto.ProcessGroupName = normFactor.ProductionProcess!.ProcessGroup!.Name;
        dto.ProcessGroupCode = normFactor.ProductionProcess!.ProcessGroup!.Code!.Value;


        return dto;
    }
}
