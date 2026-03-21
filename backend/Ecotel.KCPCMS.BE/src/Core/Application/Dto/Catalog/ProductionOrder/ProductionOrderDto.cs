using Application.Common.Interfaces;

namespace Application.Dto.Catalog.ProductionOrder;

public class CreateProductionOrderDto
{
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}

public class ProductionOrderDto : IDto
{
    public Guid Id { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}
