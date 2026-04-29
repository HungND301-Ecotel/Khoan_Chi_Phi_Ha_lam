using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.AkFactorConfig;
using Ardalis.Specification;

namespace Application.Catalog.Index.AkFactorConfig.Specifications;

public class AkFactorConfigsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.AkFactorConfig, AkFactorConfigDto>
{
    public AkFactorConfigsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? string.Empty).Trim().ToLower();

        Query.Include(x => x.ProcessGroup);

        Query
            .Where(x => string.IsNullOrWhiteSpace(searchTerm) ||
                        (x.Description != null && x.Description.ToLower().Contains(searchTerm)) ||
                        (x.AkDiffDisplay != null && x.AkDiffDisplay.ToLower().Contains(searchTerm)) ||
                        (x.AdjustmentRateDisplay != null && x.AdjustmentRateDisplay.ToLower().Contains(searchTerm)) ||
                        (x.ProcessGroup != null && x.ProcessGroup.Name.ToLower().Contains(searchTerm)) ||
                        (x.ProcessGroup != null && x.ProcessGroup.Code != null && x.ProcessGroup.Code.Value.ToLower().Contains(searchTerm)));

        Query
            .Select(x => new AkFactorConfigDto
            {
                Id = x.Id,
                ProcessGroupId = x.ProcessGroupId,
                ProcessGroupCode = x.ProcessGroup != null && x.ProcessGroup.Code != null ? x.ProcessGroup.Code.Value : string.Empty,
                ProcessGroupName = x.ProcessGroup != null ? x.ProcessGroup.Name : string.Empty,
                AkDiffOperator = x.AkDiffOperator,
                AkDiffValue = x.AkDiffValue,
                AdjustmentRate = x.AdjustmentRate,
                AkDiffDisplay = x.AkDiffDisplay,
                AdjustmentRateDisplay = x.AdjustmentRateDisplay,
                Description = x.Description,
                CreateOn = x.CreatedOn
            });
    }
}
