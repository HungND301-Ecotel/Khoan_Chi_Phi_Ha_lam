// File: Application/Catalog/AdjustmentFactor/Specifications/AdjustmentFactorsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.AdjustmentFactor;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactor.Specifications;

public class AdjustmentFactorsByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.AdjustmentFactor, AdjustmentFactorDto>
{
    public AdjustmentFactorsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(af => af.ProcessGroup).Include(af => af.Code)
            .Where(af => af.Code != null && (string.IsNullOrWhiteSpace(searchTerm) ||
                                             af.Name.ToLower().Contains(searchTerm) ||
                                             af.Code.Value.ToLower().Contains(searchTerm)));
        Query
            .Select(af => new AdjustmentFactorDto
            {
                Id = af.Id,
                Code = af.Code.Value,
                Name = af.Name,
                Type = af.Type,
                ProcessGroupId = af.ProcessGroupId,
                ProcessGroupName = af.ProcessGroup != null ? af.ProcessGroup.Name : string.Empty,
                ProcessGroupCode = af.ProcessGroup != null ? af.ProcessGroup.Code.Value : string.Empty
            });
    }
}