// File: Application/Catalog/AdjustmentFactor/Specifications/NormFactorsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.NormFactor;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactor.Specifications;

public class NormFactorsByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.NormFactor, NormFactorDto>
{
    public NormFactorsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(nf => nf.ProductionProcess).ThenInclude(pp => pp.Code)
            .Include(nf => nf.ProductionProcess).ThenInclude(pp => pp.ProcessGroup).ThenInclude(pg => pg.Code)
            .Include(nf => nf.Hardness)
            .Include(nf => nf.StoneClampRatio)
            .Where(nf => string.IsNullOrWhiteSpace(searchTerm) ||
                         nf.ProductionProcess != null && (nf.ProductionProcess.Name.ToLower().Contains(searchTerm) || nf.ProductionProcess.Code.Value.ToLower().Contains(searchTerm)));

        Query
            .Select(nf => new NormFactorDto
            {
                Id = nf.Id,
                ProcessGroupId = nf.ProductionProcess!.ProcessGroupId,
                ProcessGroupCode = nf.ProductionProcess.ProcessGroup!.Code!.Value,
                ProcessGroupName = nf.ProductionProcess.ProcessGroup.Name,
                ProductionProcessId = nf.ProductionProcessId,
                ProductionProcessCode = nf.ProductionProcess != null ? nf.ProductionProcess.Code.Value : string.Empty,
                ProductionProcessName = nf.ProductionProcess != null ? nf.ProductionProcess.Name : string.Empty,
                HardnessId = nf.HardnessId,
                HardnessName = nf.Hardness != null ? nf.Hardness.Value : string.Empty,
                StoneClampRatioId = nf.StoneClampRatioId,
                StoneClampRatioName = nf.StoneClampRatio != null ? nf.StoneClampRatio.Value : string.Empty,
                AffectAssignmentCodeIds = nf.NormFactorAssignmentCodes.Select(a => a.AssignmentCodeId).ToList(),
                Value = nf.Value,
                TargetHardnessId = nf.TargetHardnessId ?? Guid.Empty,
                TargetHardnessName = nf.TargetHardness != null ? nf.TargetHardness.Value.ToString() : string.Empty
            });

    }
}
