// File: Application/Catalog/ProductionProcess/Specifications/ProductionProcessesByPaginationSpec.cs
using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.ProductionProcess;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.ProductionProcess.Specifications;

public class ProductionProcessesByPaginationSpec
    : EntitiesByPaginationFilterSpec<Domain.Entities.Index.ProductionProcess, ProductionProcessDto>
{
    public ProductionProcessesByPaginationSpec(PaginationFilter filter, string? search) : base(filter)
    {
        var searchTerm = (search ?? "").Trim().ToLower();

        Query
            .Include(p => p.ProcessGroup).Include(p => p.Code)
            .Where(p => string.IsNullOrWhiteSpace(searchTerm) ||
                        p.Name.ToLower().Contains(searchTerm) ||
                        p.Code.Value.ToLower().Contains(searchTerm));
        Query
            .Select(p => new ProductionProcessDto
            {
                Id = p.Id,
                Code = p.Code.Value,
                Name = p.Name,
                ProcessGroupId = p.ProcessGroupId,
                ProcessGroupName = p.ProcessGroup != null ? p.ProcessGroup.Name : string.Empty
            });
    }
}