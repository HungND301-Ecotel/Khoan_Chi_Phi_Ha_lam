using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index;

public class NormFactor : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProductionProcessId { get; protected set; }
    public Guid HardnessId { get; protected set; }
    public Guid StoneClampRatioId { get; protected set; }
    public double Value { get; protected set; }
    public Guid? TargetHardnessId { get; protected set; }


    //Navigation Properties
    public virtual Hardness? TargetHardness { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual Hardness? Hardness { get; protected set; }
    public virtual StoneClampRatio? StoneClampRatio { get; protected set; }
    private IList<PlannedMaterialCost> _plannedMaterialCosts = new List<PlannedMaterialCost>();
    public virtual IReadOnlyCollection<PlannedMaterialCost> PlannedMaterialCosts => _plannedMaterialCosts.AsReadOnly();

    private IList<NormFactorAssignmentCode> _normFactorAssignmentCodes = new List<NormFactorAssignmentCode>();
    public IReadOnlyList<NormFactorAssignmentCode> NormFactorAssignmentCodes => _normFactorAssignmentCodes.ToList();


    public static NormFactor Create(Guid productionProcessId, Guid hardnessId, Guid stoneClampRatioId, double value, Guid? targetHardnessId = null)
    {
        return new NormFactor
        {
            ProductionProcessId = productionProcessId,
            HardnessId = hardnessId,
            StoneClampRatioId = stoneClampRatioId,
            Value = value,
            TargetHardnessId = targetHardnessId
        };
    }

    public void AddNormFactorAssignmentCode(IList<NormFactorAssignmentCode> list)
    {
        _normFactorAssignmentCodes.Clear();
        foreach (var item in list)
        {
            _normFactorAssignmentCodes.Add(item);
        }
    }

    public void Update(Guid productionProcessId, Guid hardnessId, Guid stoneClampRatioId, double value, Guid? targetHardnessId = null)
    {
        ProductionProcessId = productionProcessId;
        HardnessId = hardnessId;
        StoneClampRatioId = stoneClampRatioId;
        Value = value;
        TargetHardnessId = targetHardnessId;
    }
}
