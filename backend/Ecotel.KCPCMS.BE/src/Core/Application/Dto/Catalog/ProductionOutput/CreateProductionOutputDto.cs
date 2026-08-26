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
    public double PlanProductionMeters { get; set; }
    public double StandardProductionMeters { get; set; }
    public IList<CreateProductionOutputProductDto> Products { get; set; } = new List<CreateProductionOutputProductDto>();
    public IList<CreateProductionOutputTransportLineDto> TransportLines { get; set; } = new List<CreateProductionOutputTransportLineDto>();
}

public class CreateProductionOutputProductDto
{
    public Guid ProductId { get; set; }
    public double ProductionMeters { get; set; }
    public double ActualAshContent { get; set; }
}

public class CreateProductionOutputTransportLineDto
{
    public Guid ProductionProcessId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentQuality { get; set; }
    public Guid? TransportRouteId { get; set; }
    public Guid? RouteDepartmentId { get; set; }
    public Guid? HaulDistanceId { get; set; }
    public Guid? CargoTypeId { get; set; }
    public Guid? ReceivingLocationId { get; set; }
    public Guid? DumpingLocationId { get; set; }
    public double ProductionMeters { get; set; }
}
