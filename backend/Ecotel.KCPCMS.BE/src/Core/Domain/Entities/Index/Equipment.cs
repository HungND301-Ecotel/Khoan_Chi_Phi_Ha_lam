using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.EletricityUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Equipment : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public string Name { get; protected set; }
    public Guid? UnitOfMeasureId { get; protected set; }

    // Navigation properties
    public virtual UnitOfMeasure? UnitOfMeasure { get; protected set; }
    public virtual Code? Code { get; protected set; }
    private IList<Cost> _costs = new List<Cost>();
    public virtual IReadOnlyCollection<Cost> Costs => _costs.AsReadOnly();

    private IList<EquipmentPart> _equipmentParts = new List<EquipmentPart>();
    public virtual IReadOnlyCollection<EquipmentPart> EquipmentParts => _equipmentParts.AsReadOnly();

    private IList<ElectricityUnitPriceEquipment> _electricityUnitPriceEquipments = new List<ElectricityUnitPriceEquipment>();
    public virtual IReadOnlyCollection<ElectricityUnitPriceEquipment> ElectricityUnitPriceEquipments => _electricityUnitPriceEquipments.AsReadOnly();

    private IList<MaintainUnitPrice> _maintainUnitPrices = new List<MaintainUnitPrice>();
    public virtual IReadOnlyCollection<MaintainUnitPrice> MaintainUnitPrices => _maintainUnitPrices.AsReadOnly();

    // constructor
    public static Equipment Create(string code, string name, Guid? unitOfMeasureId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        return new Equipment
        {
            Code = new Code(code.ToUpper()),
            Name = name,
            UnitOfMeasureId = unitOfMeasureId
        };
    }

    public static Equipment Create(Guid id, string code, string name, Guid? unitOfMeasureId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        return new Equipment
        {
            Id = id,
            Code = new Code(code.ToUpper()),
            Name = name,
            UnitOfMeasureId = unitOfMeasureId
        };
    }

    public void Update(string code, string name, Guid? unitOfMeasureId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        if (Code != null)
        {
            Code.Value = code.ToUpper();
        }

        Name = name;
        UnitOfMeasureId = unitOfMeasureId;
    }

    public double GetCurrentCost()
    {
        var currentMonth = DateOnly.FromDateTime(DateTime.UtcNow).Month;
        return _costs.FirstOrDefault(c =>
            c.StartMonth.Month <= currentMonth && c.EndMonth.Month >= currentMonth)?.Amount ?? 0;
    }

    public double GetEffectiveDateCost(DateOnly effectiveMonth)
    {
        return _costs.FirstOrDefault(c =>
            c.StartMonth <= effectiveMonth && c.EndMonth >= effectiveMonth)?.Amount ?? 0;
    }

    public void AddCost(Cost cost)
    {
        ArgumentNullException.ThrowIfNull(cost);
        _costs.Add(cost);
    }

    public void AddCost(IList<Cost> costs)
    {
        foreach (var cost in costs)
        {
            _costs.Add(cost);
        }
    }

    public void ClearCost()
    {
        _costs.Clear();
    }

    public bool CheckChange(Equipment dto)
    {
        return !(Code?.Value == dto.Code?.Value && Name == dto.Name && UnitOfMeasureId == dto.UnitOfMeasureId);
    }
}
