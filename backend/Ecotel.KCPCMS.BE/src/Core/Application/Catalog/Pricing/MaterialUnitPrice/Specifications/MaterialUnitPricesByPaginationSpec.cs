using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.MaterialUnitPrice;
using Ardalis.Specification;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Specifications;

public sealed class MaterialUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<TunnelExcavationMaterialUnitPrice, MaterialUnitPriceDto>
{
    public MaterialUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.Code)
            .Include(m => m.ProductionProcess)
            .Include(m => m.Hardness)
            .Include(m => m.InsertItem)
            .Include(m => m.SupportStep)
            .Include(m => m.Passport)
            .Where(m => string.IsNullOrWhiteSpace(searchTerm) ||
                        m.Code.Value.ToLower().Contains(searchTerm));
        Query
        .Select(m => new MaterialUnitPriceDto
        {
            Id = m.Id,
            Code = m.Code.Value,
            HardnessId = m.HardnessId,
            HardnessName = m.Hardness!.Value,
            InsertItemId = m.InsertItemId,
            InsertItemName = m.InsertItem!.Value,
            PassportId = m.PassportId,
            PassportName = m.Passport!.GetFullname(),
            SupportStepId = m.SupportStepId,
            SupportStepName = m.SupportStep!.Value,
            ProcessId = m.ProcessId,
            ProcessName = m.ProductionProcess != null ? m.ProductionProcess.Name : string.Empty,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            TotalPrice = m.TotalPrice
        });
    }
}