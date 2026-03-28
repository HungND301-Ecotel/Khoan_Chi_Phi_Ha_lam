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
        Query.Select(po => new ProductionOutputDto
        {
            Id = po.Id,
            StartMonth = po.StartMonth,
            EndMonth = po.EndMonth,
            ProductionMeters = po.ProductionMeters,
            StandardProductionMeters = po.StandardProductionMeters,
            AcceptanceReportId = po.AcceptanceReport != null ? po.AcceptanceReport.Id : null

        });
    }
}
