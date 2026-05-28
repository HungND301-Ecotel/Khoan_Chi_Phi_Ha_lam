using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Pricing.MaterialUnitPrice;

public abstract class MaterialUnitPrice : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public Guid ProcessId { get; protected set; }
    public Guid? TechnologyId { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }
    public double OtherMaterialvalue { get; set; }
    public double TotalPrice => ApplyOtherMaterialValue(MaterialUnitPriceAssignmentCodes.Sum(m => m.TotalPrice));

    // Navigation properties chung
    public virtual Code? Code { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual Technology? Technology { get; protected set; }

    private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
    public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();

    private IList<MaterialUnitPriceAssignmentCode> _materialUnitPriceAssignmentCodes = new List<MaterialUnitPriceAssignmentCode>();
    public virtual IReadOnlyCollection<MaterialUnitPriceAssignmentCode> MaterialUnitPriceAssignmentCodes => _materialUnitPriceAssignmentCodes.AsReadOnly();

    public double GetCurrentTotalPrice(DateOnly effectiveMonth)
    {
        if (StartMonth <= effectiveMonth && EndMonth >= effectiveMonth)
        {
            double totalPrice = MaterialUnitPriceAssignmentCodes.Sum(m => m.TotalPrice);
            return ApplyOtherMaterialValue(totalPrice);
        }
        return 0;
    }

    public double ApplyOtherMaterialValue(double total)
    {
        return total * (1 + OtherMaterialvalue / 100.0);
    }

    public void AddCosts(IList<MaterialUnitPriceAssignmentCode> costs)
    {
        _materialUnitPriceAssignmentCodes.Clear();
        foreach (var item in costs)
        {
            _materialUnitPriceAssignmentCodes.Add(item);
        }
    }

    protected static void ValidateCommonFields(string code, DateOnly startMonth, DateOnly endMonth)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartMonthMustBeEarlierThanEndMonth);
        }
    }
}
