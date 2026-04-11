using Application.Dto.Catalog.AdjustmentFactorDescription;

namespace Application.Dto.Catalog.PlannedMaintainCost;

public class CreatePlannedMaintainCostDto
{
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public double TrimmingCoefficient { get; set; } = 1;
    public IList<CreatePlannedMaintainCostAdjFactorDto> Costs { get; set; } = new List<CreatePlannedMaintainCostAdjFactorDto>();
}

public class CreatePlannedMaintainCostAdjFactorDto
{
    public Guid MaintainUnitPriceId { get; set; }
    public decimal Quantity { get; set; }
    public double K6AdjustmentFactorValue { get; set; }
    public IList<Guid> AdjustmentFactorDescriptions { get; set; } = new List<Guid>();
}

public class UpdatePlannedMaintainCostDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public double TrimmingCoefficient { get; set; } = 1;
    public IList<UpdatePlannedMaintainCostAdjFactorDto> Costs { get; set; } = new List<UpdatePlannedMaintainCostAdjFactorDto>();
}

public class UpdatePlannedMaintainCostAdjFactorDto
{
    public Guid MaintainUnitPriceId { get; set; }
    public decimal Quantity { get; set; }
    public double K6AdjustmentFactorValue { get; set; }
    public IList<Guid> AdjustmentFactorDescriptions { get; set; } = new List<Guid>();
}

public class PlannedMaintainCostDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductUnitPriceId { get; set; }
    public Guid OutputId { get; set; }
    public double TrimmingCoefficient { get; set; } = 1;
    public IList<PlannedMaintainCostAdjDto> Costs { get; set; } = new List<PlannedMaintainCostAdjDto>();
}

public class PlannedMaintainCostAdjDto
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
