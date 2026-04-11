using Application.Dto.Catalog.AdjustmentFactorDescription;

namespace Application.Dto.Catalog.PlannedElectricityCost;

public class CreatePlannedElectricityCostDto
{
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public double TrimmingCoefficient { get; set; } = 1;
    public IList<CreatePlannedElectricityCostAdjFactorDto> Costs { get; set; } = new List<CreatePlannedElectricityCostAdjFactorDto>();
}

public class CreatePlannedElectricityCostAdjFactorDto
{
    public Guid ElectricityUnitPriceEquipmentId { get; set; }
    public decimal Quantity { get; set; }
    public IList<Guid> AdjustmentFactorDescriptions { get; set; } = new List<Guid>();
}

public class UpdatePlannedElectricityCostDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public double TrimmingCoefficient { get; set; } = 1;
    public IList<UpdatePlannedElectricityCostAdjFactorDto> Costs { get; set; } = new List<UpdatePlannedElectricityCostAdjFactorDto>();
}

public class UpdatePlannedElectricityCostAdjFactorDto
{
    public Guid ElectricityUnitPriceEquipmentId { get; set; }
    public decimal Quantity { get; set; }
    public IList<Guid> AdjustmentFactorDescriptions { get; set; } = new List<Guid>();
}

public class PlannedElectricityCostDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public double TrimmingCoefficient { get; set; } = 1;
    public IList<PlannedElectricityCostAdjDto> Costs { get; set; } = new List<PlannedElectricityCostAdjDto>();
}

public class PlannedElectricityCostAdjDto
{
    public Guid ElectricityUnitPriceEquipmentId { get; set; }
    public double ElectricityUnitPrice { get; set; }
    public DefaultIdType EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public double TotalPrice { get; set; }
    public IList<ElectricityAjustmentFactorDescriptionDto> AdjustmentFactorDescriptions { get; set; } = new List<ElectricityAjustmentFactorDescriptionDto>();
}
