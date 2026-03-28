using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.Material;
using Ardalis.Specification;
using Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Material.Specifications;

public class MaterialsByPaginationSpec : EntitiesByPaginationFilterSpec<Domain.Entities.Index.Material, MaterialDto>
{
    public MaterialsByPaginationSpec(PaginationFilter filter, string? search, MaterialType? type, DateTime date) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();
        var checkDate = new DateOnly(date.Year, date.Month, 1);

        Query
            .Include(m => m.UnitOfMeasure)
            .Include(m => m.AssignmentCode).ThenInclude(a => a.Code)
            .Include(m => m.Costs)
            .Include(m => m.Code)
            .Where(m =>
                m.Code != null &&
                (string.IsNullOrWhiteSpace(searchTerm) ||
                 m.Name.ToLower().Contains(searchTerm) ||
                 m.Code.Value.ToLower().Contains(searchTerm)) &&
                (type == null || m.MaterialType == type)
            );
        Query.Select(m => new MaterialDto
        {
            Id = m.Id,
            Code = m.Code.Value,
            Name = m.Name,
            UnitOfMeasureId = m.UnitOfMeasureId,
            UnitOfMeasureName = m.UnitOfMeasure != null ? m.UnitOfMeasure.Name : string.Empty,
            AssignmentCodeId = m.AssignmentCode != null ? m.AssignmentCode.Id : DefaultIdType.Empty,
            AssignmentCode = m.AssignmentCode != null && m.AssignmentCode.Code != null ? m.AssignmentCode.Code.Value : string.Empty,
            IsSlideAssignmentCode = m.AssignmentCode != null ? m.AssignmentCode.IsSlideAssignmentCode : false,
            UsageTime = m.UsageTime,
            CostAmount = m.Costs
                    .Where(c => c.CostType == CostType.Material &&
                                c.StartMonth <= checkDate &&
                                c.EndMonth >= checkDate)
                    .Select(c => c.Amount)
                    .FirstOrDefault(),
            ActualAmount = m.Costs
                    .Where(c => c.CostType == CostType.Material &&
                                c.StartMonth <= checkDate &&
                                c.EndMonth >= checkDate)
                    .Select(c => c.ActualAmount)
                    .FirstOrDefault(),
            MaterialType = m.MaterialType
        });
    }
}