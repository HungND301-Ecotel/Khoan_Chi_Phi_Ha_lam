using Domain.Common.Enums;

namespace Application.Dto.Catalog.Cost
{
    public class MaterialCostDto
    {
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public CostType CostType { get; } = CostType.Material;
        public double Amount { get; set; }
    }

    public class ElectricityCostDto
    {
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public CostType CostType { get; } = CostType.Electricity;
        public double Amount { get; set; }
    }

    public class MaintainCostDto
    {
        public DateOnly StartMonth { get; set; }
        public DateOnly EndMonth { get; set; }
        public CostType CostType { get; } = CostType.Part;
        public double Amount { get; set; }
    }

    public class CostDto
    {
        public DateTimeOffset StartMonth { get; set; }
        public DateTimeOffset EndMonth { get; set; }
        public double Amount { get; set; }
    }
}
