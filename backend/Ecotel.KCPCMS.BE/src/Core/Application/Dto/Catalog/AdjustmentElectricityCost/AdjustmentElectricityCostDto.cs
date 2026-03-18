using Application.Dto.Catalog.AdjustmentFactorDescription;

namespace Application.Dto.Catalog.AdjustmentElectricityCost;
public class AdjustmentElectricityCostDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public IList<AdjustmentElectricityCostAdjDto> Costs { get; set; } = new List<AdjustmentElectricityCostAdjDto>();
}

public class AdjustmentElectricityCostAdjDto
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