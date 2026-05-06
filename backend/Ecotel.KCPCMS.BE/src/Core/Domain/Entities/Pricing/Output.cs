using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Pricing;

public class Output : AuditableEntity<Guid>
{
    public Guid ProductUnitPriceId { get; protected set; }
    public double ProductionMeters { get; protected set; }
    public double PlanAshContent { get; protected set; }
    public OutputType OutputType { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }


    private double? CachedActualTotalPrice { get; set; }
    private double? CachedPlannedTotalPrice { get; set; }

    // Navigation Properties
    public virtual ProductUnitPrice? ProductUnitPrice { get; protected set; }
    public virtual PlannedMaterialCost? PlannedMaterialCost { get; protected set; }
    public virtual PlannedMaintainCost? PlannedMaintainCost { get; protected set; }
    public virtual PlannedElectricityCost? PlannedElectricityCost { get; protected set; }

    //Constructor
    public double GetPlannedTotalPrice()
    {
        if (CachedPlannedTotalPrice.HasValue)
        {
            return CachedPlannedTotalPrice.Value;
        }

        CachedPlannedTotalPrice =
            ProductionMeters * (
                (PlannedMaterialCost?.GetTotalPrice() ?? 0)
                + (PlannedMaintainCost?.GetPlannedTotalPrice() ?? 0)
                + (PlannedElectricityCost?.GetPlannedTotalPrice() ?? 0)
            );

        return CachedPlannedTotalPrice.Value;
    }

    public double GetAdjustmentTotalPrice(double actualProductionMeters)
    {
        return actualProductionMeters * ((PlannedMaterialCost?.GetTotalPrice() ?? 0) + (PlannedMaintainCost?.GetPlannedTotalPrice() ?? 0) + (PlannedElectricityCost?.GetPlannedTotalPrice() ?? 0));
    }

    public double GetActualTotalPrice()
    {
        return 0;
    }

    public static Output Create(double productionMeters, DateOnly startMonth, DateOnly endMonth, OutputType outputType, double planAshContent = 0)
    {
        return new Output
        {
            ProductionMeters = productionMeters,
            PlanAshContent = planAshContent,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            OutputType = outputType
        };
    }

    public static Output Create(Guid id, double productionMeters, DateOnly startMonth, DateOnly endMonth, OutputType outputType, double planAshContent = 0)
    {
        return new Output
        {
            Id = id,
            ProductionMeters = productionMeters,
            PlanAshContent = planAshContent,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            OutputType = outputType
        };
    }

    public void Update(double productionMeters, DateOnly startMonth, DateOnly endMonth, double planAshContent = 0)
    {
        ProductionMeters = productionMeters;
        PlanAshContent = planAshContent;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
    }
}
