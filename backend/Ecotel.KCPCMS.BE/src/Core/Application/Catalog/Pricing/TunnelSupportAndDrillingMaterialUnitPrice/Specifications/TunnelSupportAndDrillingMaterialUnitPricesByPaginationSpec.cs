using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MaterialUnitPrice;
using Ardalis.Specification;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Specifications;

public sealed class TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<TunnelSupportAndDrillingMaterialUnitPrice, TunnelSupportAndDrillingMaterialUnitPriceDto>
{
    public TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.Code)
            .Include(m => m.ProductionProcess)
            .Include(m => m.Hardness)
            .Include(m => m.Passport)
            .Include(m => m.Technology)
            .Include(m => m.MaterialUnitPriceAssignmentCodes)
            .Where(m => string.IsNullOrWhiteSpace(searchTerm) ||
                        m.Code.Value.ToLower().Contains(searchTerm));
        Query
        .Select(m => new TunnelSupportAndDrillingMaterialUnitPriceDto
        {
            Id = m.Id,
            Code = m.Code.Value,
            HardnessId = m.HardnessId,
            HardnessName = m.Hardness!.Value,
            TechnologyId = m.TechnologyId,
            TechnologyName = m.Technology.Value,
            PassportId = m.PassportId,
            PassportName = m.Passport!.GetFullname(),
            ProcessId = m.ProcessId,
            ProcessName = m.ProductionProcess != null ? m.ProductionProcess.Name : string.Empty,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            TotalPrice = m.TotalPrice
        });
    }
}
