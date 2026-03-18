using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.StoneClampRatio;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.StoneClampRatio.Specifications;

public class StoneClampRatiosByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.StoneClampRatio, StoneClampRatioDto>
{
    public StoneClampRatiosByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(s => s.Hardness)
            .Include(s => s.ProductionProcess).ThenInclude(pp => pp.Code)
            .Where(s => string.IsNullOrWhiteSpace(searchTerm) ||
                        s.Value.ToLower().Contains(searchTerm) ||
                        s.CoefficientValue.ToString().Contains(searchTerm) ||
                        s.Hardness != null && s.Hardness.Value.ToLower().Contains(searchTerm) ||
                        s.ProductionProcess != null &&
                         (s.ProductionProcess.Code.Value.ToLower().Contains(searchTerm) ||
                          s.ProductionProcess.Name.ToLower().Contains(searchTerm)));
        Query
            .Select(s => new StoneClampRatioDto
            {
                Id = s.Id,
                CoefficientValue = s.CoefficientValue,
                HardnessId = s.HardnessId,
                HardnessValue = s.Hardness != null ? s.Hardness.Value : string.Empty,
                ProcessId = s.ProcessId,
                ProcessCode = s.ProductionProcess != null ? s.ProductionProcess.Code.Value : string.Empty,
                ProcessName = s.ProductionProcess != null ? s.ProductionProcess.Name : string.Empty,
                Value = s.Value
            });
    }
}