using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Dto.Catalog.LongwallParameters;
using Ardalis.Specification;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Specifications;

public sealed class LongwallMaterialUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice, LongwallMaterialUnitPriceDto>
{
    public LongwallMaterialUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.Code)
            .Include(m => m.ProductionProcess)
            .Include(m => m.LongwallParameters)
            .Include(m => m.CuttingThickness)
            .Include(m => m.SeamFace)
            .Include(m => m.Technology)
            .Where(m => string.IsNullOrWhiteSpace(searchTerm) ||
                        m.Code.Value.ToLower().Contains(searchTerm));

        Query.Select(m => new LongwallMaterialUnitPriceDto
        {
            Id = m.Id,
            Code = m.Code.Value,
            ProcessId = m.ProcessId,
            ProcessName = m.ProductionProcess.Name,
            LongwallParametersId = m.LongwallParametersId,
            CuttingThicknessId = m.CuttingThicknessId,
            SeamFaceId = m.SeamFaceId,
            TechnologyId = m.TechnologyId,
            LongwallParameters = m.LongwallParameters.Adapt<LongwallParametersDto>(),
            CuttingThickness = m.CuttingThickness.Adapt<CuttingThicknessDto>(),
            SeamFaceName = m.SeamFace != null ? m.SeamFace.Value : string.Empty,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            TotalPrice = m.TotalPrice
        });
    }
}
