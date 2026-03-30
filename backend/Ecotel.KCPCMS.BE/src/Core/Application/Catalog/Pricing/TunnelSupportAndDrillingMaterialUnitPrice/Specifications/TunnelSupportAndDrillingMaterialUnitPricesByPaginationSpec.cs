using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MaterialUnitPrice;
using Ardalis.Specification;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Specifications;

public sealed class TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<TunnelSupportAndDrillingMaterialUnitPrice, MaterialUnitPriceDto>
{
    public TunnelSupportAndDrillingMaterialUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.Code)
            .Include(m => m.ProductionProcess)
            .Include(m => m.Hardness)
            .Include(m => m.Passport)
            .Include(m => m.MaterialUnitPriceAssignmentCodes)
            .Where(m => string.IsNullOrWhiteSpace(searchTerm) ||
                        m.Code.Value.ToLower().Contains(searchTerm));
        Query
        .Select(m => new MaterialUnitPriceDto
        {
            Id = m.Id,
            Code = m.Code.Value,
            HardnessId = m.HardnessId,
            HardnessName = m.Hardness!.Value,
            InsertItemId = Guid.Empty,
            InsertItemName = string.Empty,
            PassportId = m.PassportId,
            PassportName = m.Passport!.GetFullname(),
            SupportStepId = Guid.Empty,
            SupportStepName = string.Empty,
            ProcessId = m.ProcessId,
            ProcessName = m.ProductionProcess != null ? m.ProductionProcess.Name : string.Empty,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            TotalPrice = m.TotalPrice
        });
    }
}
