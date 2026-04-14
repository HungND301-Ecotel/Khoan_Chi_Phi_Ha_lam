using Application.Common.Interfaces;

namespace Application.Dto.Catalog.ProductionOutput;

public class ProductionOutputDto : IDto
{
    public Guid Id { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? AcceptanceReportId { get; set; }
    public double ProductionMeters { get; set; }
    public double StandardProductionMeters { get; set; }
    public IList<ProductionOutputProcessGroupDto> ProcessGroups { get; set; } = new List<ProductionOutputProcessGroupDto>();
}

public class ProductionOutputProcessGroupDto
{
    public Guid ProcessGroupId { get; set; }
    public string ProcessGroupCode { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public double StandardProductionMeters { get; set; }
    public double ProductionMeters { get; set; }
    public IList<ProductionOutputProductDto> Products { get; set; } = new List<ProductionOutputProductDto>();
}

public class ProductionOutputProductDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public double ProductionMeters { get; set; }
    public double ActualAshContent { get; set; }
}
