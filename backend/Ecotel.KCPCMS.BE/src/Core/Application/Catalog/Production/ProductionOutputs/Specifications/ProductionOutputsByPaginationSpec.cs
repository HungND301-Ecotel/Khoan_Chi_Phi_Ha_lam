using Application.Common.Models;
using Application.Common.Specification;
using Application.Dto.Catalog.ProductionOutput;
using Ardalis.Specification;
using Domain.Entities.Production;

namespace Application.Catalog.Production.ProductionOutputs.Specifications;

public class ProductionOutputsByPaginationSpec : EntitiesByPaginationFilterSpec<ProductionOutput, ProductionOutputDto>
{
    public ProductionOutputsByPaginationSpec(PaginationFilter filter)
        : base(filter)
    {
        Query.Include(p => p.AcceptanceReport);
        Query.Include(p => p.Department).ThenInclude(d => d.Code);
        Query.Select(po => new ProductionOutputDto
        {
            Id = po.Id,
            StartMonth = po.StartMonth,
            EndMonth = po.EndMonth,
            DepartmentId = po.DepartmentId,
            DepartmentCode = po.Department != null && po.Department.Code != null ? po.Department.Code.Value : string.Empty,
            DepartmentName = po.Department != null ? po.Department.Name : string.Empty,
            ProductionMeters = po.ProductionMeters,
            StandardProductionMeters = po.StandardProductionMeters,
            AcceptanceReportId = po.AcceptanceReport != null ? po.AcceptanceReport.Id : null

        });
    }
}
