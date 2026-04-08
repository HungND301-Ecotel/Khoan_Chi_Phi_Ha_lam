using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using Ardalis.Specification;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Specifications;

public class RevenueCostAdjustmentConfigsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.RevenueCostAdjustmentConfig, RevenueCostAdjustmentConfigDto>
{
    public RevenueCostAdjustmentConfigsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? string.Empty).Trim().ToLower();

        Query
            .Where(x => string.IsNullOrWhiteSpace(searchTerm) ||
                        (x.Description != null && x.Description.ToLower().Contains(searchTerm)) ||
                        x.ProfitConditionDisplay.ToLower().Contains(searchTerm) ||
                        x.RateDisplay.ToLower().Contains(searchTerm));

        Query
            .Select(x => new RevenueCostAdjustmentConfigDto
            {
                Id = x.Id,
                ProfitConditionDisplay = x.ProfitConditionDisplay,
                MinProfit = x.MinProfit,
                MaxProfit = x.MaxProfit,
                RateDisplay = x.RateDisplay,
                Rate = x.Rate,
                Description = x.Description,
                CreateOn = x.CreatedOn
            });
    }
}
