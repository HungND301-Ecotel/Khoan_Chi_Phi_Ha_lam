using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Dto.Catalog.NormFactor;
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

    public async Task<NormFactorDto> Handle(GetNormFactorByIdQuery request, CancellationToken cancellationToken)
    {
        var normFactor = await _normFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: p => p
                .Include(x => x.ProductionProcess).ThenInclude(pp => pp.Code)
                .Include(x => x.ProductionProcess).ThenInclude(pp => pp.ProcessGroup).ThenInclude(pg => pg.Code)
                .Include(x => x.Hardness)
                .Include(x => x.StoneClampRatio)
                .Include(x => x.NormFactorAssignmentCodes).ThenInclude(a => a.AssignmentCode).ThenInclude(c => c.Code)
                .Include(x => x.NormFactorAssignmentCodes).ThenInclude(a => a.TargetHardness),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var assignmentDtos = normFactor.NormFactorAssignmentCodes
            .Select(a => new NormFactorAssignmentCodeDto
            {
                AssignmentCodeId = a.AssignmentCodeId,
                AssignmentCode = a.AssignmentCode?.Code?.Value ?? string.Empty,
                AssignmentCodeName = a.AssignmentCode?.Name ?? string.Empty,
                Value = a.Value,
                TargetHardnessId = a.TargetHardnessId,
                TargetHardnessName = a.TargetHardness?.Value ?? string.Empty
            })
            .OrderBy(x => x.AssignmentCode)
            .ToList();

        var firstAssignment = assignmentDtos.FirstOrDefault();
        return new NormFactorDto
        {
            Id = normFactor.Id,
            ProcessGroupId = normFactor.ProductionProcess?.ProcessGroupId ?? Guid.Empty,
            ProcessGroupCode = normFactor.ProductionProcess?.ProcessGroup?.Code?.Value ?? string.Empty,
            ProcessGroupName = normFactor.ProductionProcess?.ProcessGroup?.Name ?? string.Empty,
            ProductionProcessId = normFactor.ProductionProcessId,
            ProductionProcessCode = normFactor.ProductionProcess?.Code?.Value ?? string.Empty,
            ProductionProcessName = normFactor.ProductionProcess?.Name ?? string.Empty,
            HardnessId = normFactor.HardnessId,
            HardnessName = normFactor.Hardness?.Value ?? string.Empty,
            StoneClampRatioId = normFactor.StoneClampRatioId,
            StoneClampRatioName = normFactor.StoneClampRatio?.Value ?? string.Empty,
            SteelMeshType = normFactor.SteelMeshType,
            AffectAssignmentCodes = assignmentDtos.Select(a => new ShortAssignmentCodeDto
            {
                Id = a.AssignmentCodeId,
                Code = a.AssignmentCode,
                Name = a.AssignmentCodeName
            }).ToList(),
            AssignmentCodes = assignmentDtos,
            Value = firstAssignment?.Value ?? 0,
            TargetHardnessId = firstAssignment?.TargetHardnessId,
            TargetHardnessName = firstAssignment?.TargetHardnessName ?? string.Empty
        };
    }
}
