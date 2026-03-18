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
    public double TotalPrice { get; set; }

    // Navigation properties chung
    public virtual Code? Code { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual Technology? Technology { get; protected set; }

    private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
    public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();

    public double GetCurrentTotalPrice(DateOnly effectiveMonth)
    {
        if (StartMonth <= effectiveMonth && EndMonth >= effectiveMonth)
        {
            return TotalPrice;
        }
        return 0;
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
