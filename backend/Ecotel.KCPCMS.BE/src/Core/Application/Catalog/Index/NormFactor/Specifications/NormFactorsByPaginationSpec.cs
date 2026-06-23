using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.AssignmentCode;
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
            .Include(nf => nf.NormFactorAssignmentCodes).ThenInclude(nf => nf.AssignmentCode).ThenInclude(a => a.Code)
            .Include(nf => nf.NormFactorAssignmentCodes).ThenInclude(nf => nf.Material).ThenInclude(m => m.Code)
            .Include(nf => nf.NormFactorAssignmentCodes).ThenInclude(nf => nf.TargetHardness)
            .Include(nf => nf.ProductionProcess).ThenInclude(pp => pp.Code)
            .Include(nf => nf.ProductionProcess).ThenInclude(pp => pp.ProcessGroup).ThenInclude(pg => pg.FixedKey)
            .Include(nf => nf.Hardness)
            .Include(nf => nf.StoneClampRatio)
            .Where(nf => string.IsNullOrWhiteSpace(searchTerm) ||
                         nf.ProductionProcess != null && (nf.ProductionProcess.Name.ToLower().Contains(searchTerm) || nf.ProductionProcess.Code.Value.ToLower().Contains(searchTerm)));

        Query.Select(nf => new NormFactorDto
        {
            Id = nf.Id,
            ProcessGroupId = nf.ProductionProcess!.ProcessGroupId,
            ProcessGroupCode = nf.ProductionProcess.ProcessGroup!.FixedKey!.Key,
            ProcessGroupName = nf.ProductionProcess.ProcessGroup.Name,
            ProductionProcessId = nf.ProductionProcessId,
            ProductionProcessCode = nf.ProductionProcess != null ? nf.ProductionProcess.Code.Value : string.Empty,
            ProductionProcessName = nf.ProductionProcess != null ? nf.ProductionProcess.Name : string.Empty,
            HardnessId = nf.HardnessId,
            HardnessName = nf.Hardness != null ? nf.Hardness.Value : string.Empty,
            StoneClampRatioId = nf.StoneClampRatioId,
            StoneClampRatioName = nf.StoneClampRatio != null ? nf.StoneClampRatio.Value : string.Empty,
            SteelMeshType = nf.SteelMeshType,
            AssignmentCodes = nf.NormFactorAssignmentCodes.Select(a => new NormFactorAssignmentCodeDto
            {
                AssignmentCodeId = a.AssignmentCodeId,
                AssignmentCode = a.AssignmentCode.Code!.Value,
                AssignmentCodeName = a.AssignmentCode.Name,
                MaterialId = a.MaterialId,
                MaterialCode = a.Material != null ? a.Material.Code!.Value : string.Empty,
                MaterialName = a.Material != null ? a.Material.Name : string.Empty,
                Value = a.Value,
                TargetHardnessId = a.TargetHardnessId,
                TargetHardnessName = a.TargetHardness != null ? a.TargetHardness.Value : string.Empty
            }).ToList(),
        });
    }
}