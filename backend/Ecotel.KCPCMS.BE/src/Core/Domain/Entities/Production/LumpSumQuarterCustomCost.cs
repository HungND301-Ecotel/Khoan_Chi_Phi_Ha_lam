using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class LumpSumQuarterCustomCost : AuditableEntity<Guid>, IAggregateRoot
{
    public int Quarter { get; protected set; }
    public int Year { get; protected set; }
    public Guid? ProcessGroupId { get; protected set; }
    public string CustomName { get; protected set; } = string.Empty;
    public double ActualQuantity { get; protected set; }
    public double MaterialUnitPrice { get; protected set; }
    public double MaintainUnitPrice { get; protected set; }
    public double ElectricityUnitPrice { get; protected set; }

    public virtual ProcessGroup? ProcessGroup { get; protected set; }

    public static LumpSumQuarterCustomCost Create(
        int quarter,
        int year,
        Guid? processGroupId,
        string customName,
        double actualQuantity,
        double materialUnitPrice,
        double maintainUnitPrice,
        double electricityUnitPrice)
    {
        return new LumpSumQuarterCustomCost
        {
            Quarter = quarter,
            Year = year,
            ProcessGroupId = processGroupId,
            CustomName = customName,
            ActualQuantity = actualQuantity,
            MaterialUnitPrice = materialUnitPrice,
            MaintainUnitPrice = maintainUnitPrice,
            ElectricityUnitPrice = electricityUnitPrice
        };
    }

    public void Update(
        int quarter,
        int year,
        Guid? processGroupId,
        string customName,
        double actualQuantity,
        double materialUnitPrice,
        double maintainUnitPrice,
        double electricityUnitPrice)
    {
        Quarter = quarter;
        Year = year;
        ProcessGroupId = processGroupId;
        CustomName = customName;
        ActualQuantity = actualQuantity;
        MaterialUnitPrice = materialUnitPrice;
        MaintainUnitPrice = maintainUnitPrice;
        ElectricityUnitPrice = electricityUnitPrice;
    }
}
