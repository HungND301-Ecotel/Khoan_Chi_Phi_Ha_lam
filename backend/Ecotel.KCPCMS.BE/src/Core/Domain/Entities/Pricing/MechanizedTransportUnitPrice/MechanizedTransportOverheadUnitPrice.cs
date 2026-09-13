using System;
using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Pricing.MechanizedTransportUnitPrice;

public class MechanizedTransportOverheadUnitPrice : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProcessGroupId { get; protected set; }
    public Guid DepartmentId { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public decimal LowValuePerishableSupplyUnitPrice { get; protected set; }
    public decimal? ElectricityUnitPrice { get; protected set; }

    public virtual ProcessGroup? ProcessGroup { get; protected set; }
    public virtual Department? Department { get; protected set; }

    public static MechanizedTransportOverheadUnitPrice Create(
        Guid processGroupId,
        Guid departmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        decimal lowValuePerishableSupplyUnitPrice,
        decimal? electricityUnitPrice)
    {
        Validate(processGroupId, departmentId, startMonth, endMonth, lowValuePerishableSupplyUnitPrice, electricityUnitPrice);

        return new MechanizedTransportOverheadUnitPrice
        {
            ProcessGroupId = processGroupId,
            DepartmentId = departmentId,
            StartMonth = startMonth,
            EndMonth = endMonth,
            LowValuePerishableSupplyUnitPrice = lowValuePerishableSupplyUnitPrice,
            ElectricityUnitPrice = electricityUnitPrice
        };
    }

    public void Update(
        Guid processGroupId,
        Guid departmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        decimal lowValuePerishableSupplyUnitPrice,
        decimal? electricityUnitPrice)
    {
        Validate(processGroupId, departmentId, startMonth, endMonth, lowValuePerishableSupplyUnitPrice, electricityUnitPrice);

        ProcessGroupId = processGroupId;
        DepartmentId = departmentId;
        StartMonth = startMonth;
        EndMonth = endMonth;
        LowValuePerishableSupplyUnitPrice = lowValuePerishableSupplyUnitPrice;
        ElectricityUnitPrice = electricityUnitPrice;
    }

    private static void Validate(
        Guid processGroupId,
        Guid departmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        decimal lowValuePerishableSupplyUnitPrice,
        decimal? electricityUnitPrice)
    {
        if (processGroupId == Guid.Empty)
        {
            throw new ArgumentException("Nhóm công đoạn sản xuất không được để trống.");
        }
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException("Đơn vị không được để trống.");
        }
        if (startMonth > endMonth)
        {
            throw new ArgumentException("Thời gian bắt đầu không được lớn hơn thời gian kết thúc.");
        }
        if (lowValuePerishableSupplyUnitPrice < 0)
        {
            throw new ArgumentException("Đơn giá vật tư mau hỏng rẻ tiền không được là số âm.");
        }
        if (electricityUnitPrice is < 0)
        {
            throw new ArgumentException("Đơn giá điện năng không được là số âm.");
        }
    }
}