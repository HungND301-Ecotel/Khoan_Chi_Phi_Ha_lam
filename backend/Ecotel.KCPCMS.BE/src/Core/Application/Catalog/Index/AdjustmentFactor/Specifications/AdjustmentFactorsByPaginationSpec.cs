// File: Application/Catalog/AdjustmentFactor/Specifications/AdjustmentFactorsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.AdjustmentFactor;
using Ardalis.Specification;
using Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactor.Specifications;

public class AdjustmentFactorsByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.AdjustmentFactor, AdjustmentFactorDto>
{
    public AdjustmentFactorsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(af => af.ProcessGroup).ThenInclude(pg => pg.FixedKey).Include(af => af.FixedKey).Include(af => af.Code)
            .Where(af => af.Code != null && (string.IsNullOrWhiteSpace(searchTerm) ||
                                             af.Name.ToLower().Contains(searchTerm) ||
                                             af.Code.Value.ToLower().Contains(searchTerm) ||
                                             (af.FixedKey != null && af.FixedKey.Key.ToLower().Contains(searchTerm)) ||
                                             (af.ProcessGroup != null && af.ProcessGroup.FixedKey != null && af.ProcessGroup.FixedKey.Key.ToLower().Contains(searchTerm))));
        Query
            .Select(af => new AdjustmentFactorDto
            {
                Id = af.Id,
                Code = af.FixedKey != null ? af.FixedKey.Key : af.Code.Value,
                FixedKeyId = af.FixedKeyId,
                FixedKeyKey = af.FixedKey != null ? af.FixedKey.Key : af.Code.Value,
                FixedKeyType = af.FixedKey != null ? af.FixedKey.Type : Domain.Common.Enums.FixedKeyType.None,
                Name = af.Name,
                Type = af.FixedKey != null ? af.FixedKey.Type.ToAdjustmentFactorType() : Domain.Common.Enums.AdjustmentFactorType.None,
                ProcessGroupId = af.ProcessGroupId,
                ProcessGroupName = af.ProcessGroup != null ? af.ProcessGroup.Name : string.Empty,
                ProcessGroupCode = af.ProcessGroup != null && af.ProcessGroup.FixedKey != null ? af.ProcessGroup.FixedKey.Key : string.Empty
            });
    }
}