using Application.Common.Models;
using Application.Common.Specification;
using Ardalis.Specification;
using Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ProductUnitPrice.Specifications;

public sealed class ProductUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.ProductUnitPrice>
{
    public ProductUnitPricesByPaginationSpec(PaginationFilter filter, string? search, OutputType outputType) : base(filter)
    {
        var searchTerm = (search ?? "").Trim();

        Query
            .AsSplitQuery()
            .AsNoTracking()
            .Include(e => e.UnitOfMeasure)
            .Include(e => e.Product)
                .ThenInclude(p => p.Code)
            .Include(e => e.Product)
                .ThenInclude(p => p.ProcessGroup)
                    .ThenInclude(pg => pg.Code);


        // Outputs → PlannedMaterialCost
        Query.Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedMaterialCost)
                .ThenInclude(c => c.MaterialUnitPrice)
        .Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedMaterialCost)
                .ThenInclude(c => c.SlideUnitPriceAssignmentCode)
                    .ThenInclude(c => c.Material)
                        .ThenInclude(m => m.Costs)
        .Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedMaterialCost)
                .ThenInclude(c => c.StoneClampRatio);

        // Outputs → PlannedMaintainCost
        Query.Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedMaintainCost)
                .ThenInclude(c => c.PlannedMaintainCostAdjustmentFactors)
                    .ThenInclude(f => f.MaintainUnitPrice)
                        .ThenInclude(mu => mu.MaintainUnitPriceEquipments)
                            .ThenInclude(e => e.Part)
                                .ThenInclude(p => p.Costs)
        .Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedMaintainCost)
                .ThenInclude(c => c.PlannedMaintainCostAdjustmentFactors)
                    .ThenInclude(f => f.PlannedMaintainCostAdjustmentFactorDescriptions)
                        .ThenInclude(d => d.AdjustmentFactorDescription);

        // Outputs → PlannedElectricityCost
        Query.Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedElectricityCost)
                .ThenInclude(c => c.PlannedElectricityCostAdjustmentFactors)
                    .ThenInclude(f => f.ElectricityUnitPriceEquipment)
                        .ThenInclude(eq => eq.Equipment)
                            .ThenInclude(e => e.Costs)
        .Include(e => e.Outputs)
            .ThenInclude(o => o.PlannedElectricityCost)
                .ThenInclude(c => c.PlannedElectricityCostAdjustmentFactors)
                    .ThenInclude(f => f.PlannedElectricityCostAdjustmentFactorDescriptions)
                        .ThenInclude(d => d.AdjustmentFactorDescription);

        if (outputType == OutputType.ActualOutput)
        {
            // ActualOutput no longer has separate cost entities
        }

        Query.Where(m =>
            m.Product != null &&
            (string.IsNullOrWhiteSpace(searchTerm) ||
            m.Product.Name.Like($"%{searchTerm}%") ||
            m.Product.Code.Value.Like($"%{searchTerm}%")
        ));
    }
}
