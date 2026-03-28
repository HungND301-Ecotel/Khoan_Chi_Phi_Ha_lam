namespace Application.Dto.Catalog.ActualElectricityCost;

public class CreateActualElectricityCostDto
{
    public Guid AcceptanceReportId { get; set; }
    public IList<CreateActualElectricityEquipmentDto> Equipments { get; set; } = new List<CreateActualElectricityEquipmentDto>();
}

public class CreateActualElectricityEquipmentDto
{
    public Guid EquipmentId { get; set; }
    public double ActualElectricityConsumption { get; set; }
}

public class UpdateActualElectricityCostDto
{
    public Guid Id { get; set; }
    public Guid AcceptanceReportId { get; set; }
    public IList<UpdateActualElectricityEquipmentDto> Equipments { get; set; } = new List<UpdateActualElectricityEquipmentDto>();
}

public class UpdateActualElectricityEquipmentDto
{
    public Guid EquipmentId { get; set; }
    public double ActualElectricityConsumption { get; set; }
}

public class ActualElectricityCostDetailDto
{
    public Guid Id { get; set; }
    public Guid AcceptanceReportId { get; set; }
    public IList<ActualElectricityEquipmentDetailDto> Equipments { get; set; } = new List<ActualElectricityEquipmentDetailDto>();
}

public class ActualElectricityEquipmentDetailDto
{
    public Guid EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public double ElectricityUnitPrice { get; set; }
    public double ActualElectricityConsumption { get; set; }
    public double TotalPrice { get; set; }
}
