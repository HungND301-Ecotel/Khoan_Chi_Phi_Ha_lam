// File: Application/Catalog/AdjustmentFactorDescription/Specifications/AdjustmentFactorDescriptionsByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Specifications;

public class AdjustmentFactorDescriptionsByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.AdjustmentFactorDescription, AdjustmentFactorDescriptionDto>
{
    public AdjustmentFactorDescriptionsByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(ad => ad.AdjustmentFactor).ThenInclude(af => af.Code)
            .Where(ad => string.IsNullOrWhiteSpace(searchTerm) ||
                         ad.Description.ToLower().Contains(searchTerm) ||
                         ad.AdjustmentFactor.Code != null && ad.AdjustmentFactor.Code.Value.ToLower().Contains(searchTerm));
        Query
            .Select(ad => new AdjustmentFactorDescriptionDto
            {
                Id = ad.Id,
                AdjustmentFactorId = ad.AdjustmentFactorId,
                Description = ad.Description,
                AdjustmentFactorCode = ad.AdjustmentFactor != null ? $"{ad.AdjustmentFactor.ProcessGroup.Code.Value} - {ad.AdjustmentFactor.Code.Value}" : string.Empty,
                ElectricityAdjustmentValue = ad.ElectricityAdjustmentValue,
                MaintenanceAdjustmentValue = ad.MaintenanceAdjustmentValue
            });
    }
}