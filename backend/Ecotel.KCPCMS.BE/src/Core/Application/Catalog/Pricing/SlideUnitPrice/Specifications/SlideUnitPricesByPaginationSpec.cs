// File: Application/Catalog/Pricing/SlideUnitPrice/Specifications/SlideUnitPricesByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.SlideUnitPrice;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.SlideUnitPrice.Specifications;

public sealed class SlideUnitPricesByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.SlideUnitPrice, SlideUnitPriceDto>
{
    public SlideUnitPricesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(s => s.ProcessGroup)
            .Include(s => s.Code)
            .Include(s => s.SlideUnitPriceAssignmentCodes).ThenInclude(s => s.Material).ThenInclude(m => m.Costs)
            .Where(s => string.IsNullOrWhiteSpace(searchTerm) ||
                        s.Code.Value.ToLower().Contains(searchTerm));
        Query
        .Select(s => new SlideUnitPriceDto
        {
            Id = s.Id,
            Code = s.Code.Value,
            HardnessId = s.HardnessId,
            HardnessName = s.Hardness!.Value,
            PassportId = s.PassportId,
            PassportName = s.Passport!.GetFullname(),
            ProcessGroupId = s.ProcessGroupId,
            ProcessGroupName = s.ProcessGroup != null ? s.ProcessGroup.Name : string.Empty,
            StartMonth = s.StartMonth,
            EndMonth = s.EndMonth,
            TotalPrice = s.GetCurrentTotalPrice()
        });
    }
}