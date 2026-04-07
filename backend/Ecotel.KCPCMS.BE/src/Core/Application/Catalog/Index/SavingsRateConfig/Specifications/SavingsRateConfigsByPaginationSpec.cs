using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.SavingsRateConfig;
using Ardalis.Specification;

namespace Application.Catalog.Index.SavingsRateConfig.Specifications;

public class SavingsRateConfigsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.SavingsRateConfig, SavingsRateConfigDto>
{
    public SavingsRateConfigsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? string.Empty).Trim().ToLower();

        Query
            .Where(x => string.IsNullOrWhiteSpace(searchTerm) ||
                        (x.Description != null && x.Description.ToLower().Contains(searchTerm)) ||
                        (x.RevenueDisplay != null && x.RevenueDisplay.ToLower().Contains(searchTerm)) ||
                        (x.SavingsRateDisplay != null && x.SavingsRateDisplay.ToLower().Contains(searchTerm)));

        Query
            .Select(x => new SavingsRateConfigDto
            {
                Id = x.Id,
                MinRevenue = x.MinRevenue,
                MaxRevenue = x.MaxRevenue,
                MinSavingsRate = x.MinSavingsRate,
                MaxSavingsRate = x.MaxSavingsRate,
                RevenueDisplay = x.RevenueDisplay,
                SavingsRateDisplay = x.SavingsRateDisplay,
                Description = x.Description,
                CreateOn = x.CreatedOn
            });
    }
}
