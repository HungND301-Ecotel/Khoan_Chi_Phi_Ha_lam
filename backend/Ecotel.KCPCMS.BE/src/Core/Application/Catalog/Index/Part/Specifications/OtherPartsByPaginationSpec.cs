using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Part;
using Ardalis.Specification;
using Domain.Common.Enums;

namespace Application.Catalog.Index.Part.Specifications;

public class OtherPartsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Part, OtherPartDto>
{
    public OtherPartsByPaginationSpec(PaginationFilter payload, string? search, DateTime date) : base(payload)
    {
        var searchTerm = (search ?? "").Trim().ToLower();
        var checkDate = new DateOnly(date.Year, date.Month, 1);

        Query
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Code)
            .Include(p => p.Costs)
            .Where(p => (string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.Code.Value.ToLower().Contains(searchTerm)) && p.Type == PartType.OtherPart);
        Query
            .Select(p => new OtherPartDto
            {
                Id = p.Id,
                Code = p.Code.Value,
                Name = p.Name,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureName = p.UnitOfMeasure != null ? p.UnitOfMeasure.Name : string.Empty,
                ReplacementTimeStandard = p.ReplacementTimeStandard,
                CostAmount = p.Costs
                    .Where(c => c.CostType == CostType.Part &&
                                c.StartMonth <= checkDate &&
                                c.EndMonth >= checkDate)
                    .Select(c => c.Amount)
                    .FirstOrDefault(),
                ActualAmount = p.Costs
                    .Where(c => c.CostType == CostType.Part &&
                                c.StartMonth <= checkDate &&
                                c.EndMonth >= checkDate)
                    .Select(c => c.ActualAmount)
                    .FirstOrDefault(),
            });
    }
}
