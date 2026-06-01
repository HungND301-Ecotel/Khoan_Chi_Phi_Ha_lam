using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.AssignmentCode;
using Ardalis.Specification;
using Domain.Common.Enums;
using Domain.Entities.Index;

namespace Application.Catalog.Index.AssignmentCodes.Specifications;
public class AssignmentCodesByPaginationSpec : EntitiesByPaginationFilterSpec<AssignmentCode, AssignmentCodeDto>
{
    public AssignmentCodesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        string searchTerm = search?.Trim().ToLower() ?? string.Empty;

        Query
            .Include(x => x.UnitOfMeasure).Include(x => x.Code).Include(x => x.Costs)
            .Where(x => x.Code != null && (string.IsNullOrEmpty(searchTerm) ||
                                           x.Name.ToLower().Contains(searchTerm) ||
                                           x.Code.Value.ToLower().Contains(searchTerm)));

        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);

        Query.Select(x => new AssignmentCodeDto
        {
            Id = x.Id,
            Code = x.Code.Value,
            Name = x.Name,
            UnitOfMeasureId = x.UnitOfMeasureId,
            UnitOfMeasureName = x.UnitOfMeasure != null ? x.UnitOfMeasure.Name : string.Empty,
            CurrentPrice = x.Costs
                .Where(c => c.CostType == CostType.Electricity && c.StartMonth <= currentDate && c.EndMonth >= currentDate)
                .Select(c => c.Amount)
                .FirstOrDefault()
        });
    }
}
