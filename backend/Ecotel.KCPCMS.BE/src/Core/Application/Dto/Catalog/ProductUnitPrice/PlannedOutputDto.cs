using Domain.Common.Enums;

namespace Application.Dto.Catalog.ProductUnitPrice;

public class CreateOutputDto
{
    public double ProductionMeters { get; set; }
    public double PlanAshContent { get; set; }
    public OutputType OutputType { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
}

public class UpdateOutputDto
{
    public Guid Id { get; set; } = Guid.Empty;
    public double ProductionMeters { get; set; }
    public double PlanAshContent { get; set; }
    public OutputType OutputType { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
}

public class PlannedOutputDto
{
    public Guid Id { get; set; }
    public double ProductionMeters { get; set; }
    public double PlanAshContent { get; set; }
    public Guid? PlannedMaterialCostId { get; set; }
    public Guid? PlannedMaintainCostId { get; set; }
    public Guid? PlannedElectricityCostId { get; set; }
    public OutputType OutputType { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public double TotalPrice { get; set; }
}

public class ActualOutputDto
{
    public Guid Id { get; set; }
    public double ProductionMeters { get; set; }
    public double PlanAshContent { get; set; }
    public OutputType OutputType { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public double TotalPrice { get; set; }
    public double AdjTotalPrice { get; set; }
}
