using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing;

public class LowValuePerishableSupplyUnitPrice : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid DepartmentId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public LowValuePerishableSupplyType Type { get; protected set; } = LowValuePerishableSupplyType.TunnelExcavation;
    public double TotalPrice { get; protected set; }

    public virtual Department? Department { get; protected set; }
    public virtual ProcessGroup? ProcessGroup { get; protected set; }

    public static LowValuePerishableSupplyUnitPrice Create(
        Guid departmentId,
        Guid processGroupId,
        DateOnly startMonth,
        DateOnly endMonth,
        LowValuePerishableSupplyType type,
        double totalPrice)
    {
        Validate(startMonth, endMonth, totalPrice);

        return new LowValuePerishableSupplyUnitPrice
        {
            DepartmentId = departmentId,
            ProcessGroupId = processGroupId,
            StartMonth = startMonth,
            EndMonth = endMonth,
            Type = type,
            TotalPrice = totalPrice,
        };
    }

    public void Update(
        Guid departmentId,
        Guid processGroupId,
        DateOnly startMonth,
        DateOnly endMonth,
        LowValuePerishableSupplyType type,
        double totalPrice)
    {
        Validate(startMonth, endMonth, totalPrice);

        DepartmentId = departmentId;
        ProcessGroupId = processGroupId;
        StartMonth = startMonth;
        EndMonth = endMonth;
        Type = type;
        TotalPrice = totalPrice;
    }

    public double GetCurrentTotalPrice(DateOnly effectiveMonth)
    {
        return StartMonth <= effectiveMonth && EndMonth >= effectiveMonth
            ? TotalPrice
            : 0;
    }

    private static void Validate(DateOnly startMonth, DateOnly endMonth, double totalPrice)
    {
        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }

        if (totalPrice < 0)
        {
            throw new ArgumentException(CustomResponseMessage.AmountCannotBeNegative);
        }
    }
}
