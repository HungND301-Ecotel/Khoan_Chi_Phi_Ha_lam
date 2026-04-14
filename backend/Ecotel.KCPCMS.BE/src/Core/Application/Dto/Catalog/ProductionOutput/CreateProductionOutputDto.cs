namespace Application.Dto.Catalog.ProductionOutput;

public class CreateProductionOutputDto
{
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public double ProductionMeters { get; set; }
    public double StandardProductionMeters { get; set; }
    public Guid? DepartmentId { get; set; }
    public IList<CreateProductionOutputProcessGroupDto> ProcessGroups { get; set; } = new List<CreateProductionOutputProcessGroupDto>();
}

public class CreateProductionOutputProcessGroupDto
{
    public Guid ProcessGroupId { get; set; }
    public double StandardProductionMeters { get; set; }
    public IList<CreateProductionOutputProductDto> Products { get; set; } = new List<CreateProductionOutputProductDto>();
}

public class CreateProductionOutputProductDto
{
    public Guid ProductId { get; set; }
    public double ProductionMeters { get; set; }
    public double ActualAshContent { get; set; }
}
