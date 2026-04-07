using Application.Common.Models;
using Application.Dto.Catalog.MaterialUnitPrice;
using Ardalis.Specification;
using Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;
using DomainEntities = Domain.Entities.Pricing.MaterialUnitPrice;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Specifications;

public sealed class AllMaterialUnitPricesByPaginationSpec
    : Specification<DomainEntities.MaterialUnitPrice, AllMaterialUnitPricesDto>
{
    public AllMaterialUnitPricesByPaginationSpec(PaginationFilter filter, string? search, MaterialUnitPriceType? type = null)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(m => m.Code)
            .Include(m => m.ProductionProcess)
            .Include(m => m.Technology)
            .Include(m => m.MaterialUnitPriceAssignmentCodes)
            .Where(m => string.IsNullOrWhiteSpace(searchTerm) ||
                        m.Code.Value.ToLower().Contains(searchTerm))
            .OrderByDescending(m => m.CreatedOn);

        // Apply type filter
        if (type == MaterialUnitPriceType.Longwall)
        {
            Query.Where(m => m is DomainEntities.LongwallMaterialUnitPrice);
        }
        else if (type == MaterialUnitPriceType.TunnelExcavation)
        {
            Query.Where(m => m is DomainEntities.TunnelExcavationMaterialUnitPrice);
        }

        if (filter.PageNumber > 0 && filter.PageSize > 0 && !filter.IgnorePagination)
        {
            Query.Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        Query.Select(m => new AllMaterialUnitPricesDto
        {
            Id = m.Id,
            Code = m.Code.Value,
            ProcessId = m.ProcessId,
            ProcessName = m.ProductionProcess.Name,
            StartMonth = m.StartMonth,
            EndMonth = m.EndMonth,
            TotalPrice = m.TotalPrice,
            TechnologyId = m.TechnologyId,
            TechnologyName = m.Technology != null ? m.Technology.Value : null,
            Type = m is DomainEntities.LongwallMaterialUnitPrice ? MaterialUnitPriceType.Longwall : MaterialUnitPriceType.TunnelExcavation,

            // Longwall Properties
            LongwallParametersId = m is DomainEntities.LongwallMaterialUnitPrice ? ((DomainEntities.LongwallMaterialUnitPrice)m).LongwallParametersId : null,
            LongwallParametersName = m is DomainEntities.LongwallMaterialUnitPrice
                ? ((DomainEntities.LongwallMaterialUnitPrice)m).LongwallParameters!.Llc + "-" +
                  ((DomainEntities.LongwallMaterialUnitPrice)m).LongwallParameters!.Lkc + "-" +
                  ((DomainEntities.LongwallMaterialUnitPrice)m).LongwallParameters!.Mk
                : null,
            CuttingThicknessId = m is DomainEntities.LongwallMaterialUnitPrice ? ((DomainEntities.LongwallMaterialUnitPrice)m).CuttingThicknessId : null,
            CuttingThicknessName = m is DomainEntities.LongwallMaterialUnitPrice ? ((DomainEntities.LongwallMaterialUnitPrice)m).CuttingThickness!.Value : null,
            SeamFaceId = m is DomainEntities.LongwallMaterialUnitPrice ? ((DomainEntities.LongwallMaterialUnitPrice)m).SeamFaceId : null,
            SeamFaceName = m is DomainEntities.LongwallMaterialUnitPrice ? ((DomainEntities.LongwallMaterialUnitPrice)m).SeamFace!.Value : null,
            PowerId = m is DomainEntities.LongwallMaterialUnitPrice ? ((DomainEntities.LongwallMaterialUnitPrice)m).PowerId : null,
            PowerName = m is DomainEntities.LongwallMaterialUnitPrice
                ? (((DomainEntities.LongwallMaterialUnitPrice)m).Power != null
                    ? ((DomainEntities.LongwallMaterialUnitPrice)m).Power!.Value
                    : null)
                : null,
            IsLongwallMaterialUnitPriceCGH = m is DomainEntities.LongwallMaterialUnitPrice
                ? ((DomainEntities.LongwallMaterialUnitPrice)m).IsLongwallMaterialUnitPriceCGH
                : null,

            // TunnelExcavation Properties
            PassportId = m is DomainEntities.TunnelExcavationMaterialUnitPrice ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).PassportId : null,
            PassportName = m is DomainEntities.TunnelExcavationMaterialUnitPrice ?
                $"H/c {((DomainEntities.TunnelExcavationMaterialUnitPrice)m).Passport!.Name}; {((DomainEntities.TunnelExcavationMaterialUnitPrice)m).Passport!.Sd}; {((DomainEntities.TunnelExcavationMaterialUnitPrice)m).Passport!.Sc} "
                : null,
            HardnessId = m is DomainEntities.TunnelExcavationMaterialUnitPrice
                ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).HardnessId
                : (m is DomainEntities.LongwallMaterialUnitPrice
                    ? ((DomainEntities.LongwallMaterialUnitPrice)m).HardnessId
                    : null),
            HardnessName = m is DomainEntities.TunnelExcavationMaterialUnitPrice
                ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).Hardness!.Value
                : (m is DomainEntities.LongwallMaterialUnitPrice
                    ? (((DomainEntities.LongwallMaterialUnitPrice)m).Hardness != null
                        ? ((DomainEntities.LongwallMaterialUnitPrice)m).Hardness!.Value
                        : null)
                    : null),
            InsertItemId = m is DomainEntities.TunnelExcavationMaterialUnitPrice ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).InsertItemId : null,
            InsertItemName = m is DomainEntities.TunnelExcavationMaterialUnitPrice ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).InsertItem!.Value : null,
            SupportStepId = m is DomainEntities.TunnelExcavationMaterialUnitPrice ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).SupportStepId : null,
            SupportStepName = m is DomainEntities.TunnelExcavationMaterialUnitPrice ? ((DomainEntities.TunnelExcavationMaterialUnitPrice)m).SupportStep!.Value : null
        });
    }
}
