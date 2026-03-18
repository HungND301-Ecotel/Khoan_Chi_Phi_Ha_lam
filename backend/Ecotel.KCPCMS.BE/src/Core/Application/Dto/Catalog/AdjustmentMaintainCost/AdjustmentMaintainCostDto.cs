using Application.Dto.Catalog.AdjustmentFactorDescription;

namespace Application.Dto.Catalog.AdjustmentMaintainCost;

public class AdjustmentMaintainCostDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public IList<AdjustmentMaintainCostAdjDto> Costs { get; set; } = new List<AdjustmentMaintainCostAdjDto>();
}

public class AdjustmentMaintainCostAdjDto
{
    public Guid MaintainUnitPriceId { get; set; }
    public double MaintainUnitPrice { get; set; }
    public DefaultIdType EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public double K6AdjustmentFactorValue { get; set; }
    public double TotalPrice { get; set; }
    public IList<MaintainAjustmentFactorDescriptionDto> AdjustmentFactorDescriptions { get; set; } = new List<MaintainAjustmentFactorDescriptionDto>();
}