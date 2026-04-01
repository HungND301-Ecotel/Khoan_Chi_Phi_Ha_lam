using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.SlideUnitPrice;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.SlideUnitPrice.Specifications;

public sealed class SlideUnitPriceAssignmentCodesByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Pricing.SlideUnitPriceAssignmentCode, SlideUnitPriceAssignmentCodeDto>
{
    public SlideUnitPriceAssignmentCodesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(c => c.SlideUnitPrice).ThenInclude(s => s.Code)
            .Include(c => c.Material).ThenInclude(m => m.Costs)
            .Include(c => c.Material).ThenInclude(m => m.Code)
            .Where(c => string.IsNullOrWhiteSpace(searchTerm) ||
                        c.SlideUnitPrice.Code.Value.ToLower().Contains(searchTerm));

        Query.Select(c => new SlideUnitPriceAssignmentCodeDto
        {
            Id = c.Id, // ID của AssignmentCode, không lo bị Empty nữa
            SlideUnitPriceId = c.SlideUnitPriceId,
            HardnessId = c.SlideUnitPrice.HardnessId,
            PassportId = c.SlideUnitPrice.PassportId,
            MaterialId = c.MaterialId,
            ProcessGroupId = c.SlideUnitPrice.ProcessGroupId,
            StartMonth = c.SlideUnitPrice.StartMonth,
            EndMonth = c.SlideUnitPrice.EndMonth,
            Amount = c.Amount,
        });
    }
}