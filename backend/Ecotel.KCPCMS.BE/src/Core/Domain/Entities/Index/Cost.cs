using Domain.Common.Contracts;
using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Cost : AuditableEntity<Guid>
{
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public CostType CostType { get; protected set; }
    public double Amount { get; protected set; }
    public double ActualAmount { get; protected set; }
    public Guid? PartId { get; protected set; }
    public Guid? MaterialId { get; protected set; }
    public Guid? EquipmentId { get; protected set; }

    // Navigation properties
    public virtual Part? Part { get; protected set; }
    public virtual Material? Material { get; protected set; }
    public virtual Equipment? Equipment { get; protected set; }

    //constructor
    public static Cost Create(DateOnly startMonth, DateOnly endMonth, CostType costType, double amount, Guid costTypeId, double actualAmount = 0)
    {
        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }

        var cost = new Cost
        {
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            CostType = costType,
            Amount = amount,
            ActualAmount = actualAmount
        };

        switch (costType)
        {
            case CostType.Material:
                cost.MaterialId = costTypeId;
                break;
            case CostType.Electricity:
                cost.EquipmentId = costTypeId;
                break;

            case CostType.Part:
                cost.PartId = costTypeId;
                break;

            default:
                throw new ArgumentOutOfRangeException(CustomResponseMessage.UnsupportedCostType);
        }
        return cost;
    }
}
