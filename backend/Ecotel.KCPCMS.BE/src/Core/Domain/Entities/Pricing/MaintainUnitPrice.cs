using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing;

public class MaintainUnitPrice : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid EquipmentId { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public double? OtherMaterialValue { get; set; }
    public MaintainUnitPriceType Type { get; protected set; }

    private double? CachedMaintainUnitPriceTotal { get; set; }

    // Navigation properties
    public virtual Equipment? Equipment { get; protected set; }

    private IList<MaintainUnitPriceEquipment> _maintainUnitPriceEquipments = new List<MaintainUnitPriceEquipment>();
    public virtual IReadOnlyCollection<MaintainUnitPriceEquipment> MaintainUnitPriceEquipments => _maintainUnitPriceEquipments.AsReadOnly();

    private IList<PlannedMaintainCostAdjustmentFactor> _plannedMaintainCostAdjustmentFactors = new List<PlannedMaintainCostAdjustmentFactor>();
    public virtual IReadOnlyCollection<PlannedMaintainCostAdjustmentFactor> PlannedMaintainCostAdjustmentFactors => _plannedMaintainCostAdjustmentFactors.AsReadOnly();

    //Constructor
    public static MaintainUnitPrice Create(
        Guid equipmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        IList<MaintainUnitPriceEquipment>? maintainUnitPriceEquipments,
        double? otherMaterialValue,
        MaintainUnitPriceType type)
    {
        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }
        var maintainUnitPrice = new MaintainUnitPrice
        {
            EquipmentId = equipmentId,
            StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1),
            EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1),
            OtherMaterialValue = otherMaterialValue,
            Type = type
        };
        foreach (var item in maintainUnitPriceEquipments ?? new List<MaintainUnitPriceEquipment>())
        {
            maintainUnitPrice._maintainUnitPriceEquipments.Add(item);
        }
        return maintainUnitPrice;
    }

    public void Update(
        Guid equipmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        IList<MaintainUnitPriceEquipment>? maintainUnitPriceEquipments,
        double? otherMaterialValue,
        MaintainUnitPriceType type)
    {
        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }

        EquipmentId = equipmentId;
        StartMonth = new DateOnly(startMonth.Year, startMonth.Month, 1);
        EndMonth = new DateOnly(endMonth.Year, endMonth.Month, 1);
        OtherMaterialValue = otherMaterialValue;
        Type = type;
        _maintainUnitPriceEquipments.Clear();
        foreach (var item in maintainUnitPriceEquipments ?? new List<MaintainUnitPriceEquipment>())
        {
            _maintainUnitPriceEquipments.Add(item);
        }
    }

    public void AddMaintainUnitPriceEquipment(MaintainUnitPriceEquipment item)
    {
        _maintainUnitPriceEquipments.Add(item);
    }

    public void AddMaintainUnitPriceEquipment(IList<MaintainUnitPriceEquipment> items)
    {
        foreach (var item in items)
        {
            _maintainUnitPriceEquipments.Add(item);
        }
    }

    public void ClearMaintainUnitPriceEquipment()
    {
        _maintainUnitPriceEquipments.Clear();
    }

    public double GetMaintainTotalPrice()
    {
        if (CachedMaintainUnitPriceTotal.HasValue)
        {
            return CachedMaintainUnitPriceTotal.Value;
        }

        double result = MaintainUnitPriceEquipments.Sum(m => m.GetMaterialCostPerMetres(StartMonth));

        if (OtherMaterialValue != null)
        {
            result = result + result * ((double)OtherMaterialValue / 100);
        }

        CachedMaintainUnitPriceTotal = result;
        return CachedMaintainUnitPriceTotal.Value;
    }
}
