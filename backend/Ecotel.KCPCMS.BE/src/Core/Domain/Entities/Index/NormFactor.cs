using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class NormFactor : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid ProductionProcessId { get; protected set; }
    public Guid HardnessId { get; protected set; }
    public Guid StoneClampRatioId { get; protected set; }
    public double Value { get; protected set; }
    public Guid? ReferenceNormFactorId { get; protected set; }


    //Navigation Properties
    public virtual NormFactor? ReferenceNormFactor { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual Hardness? Hardness { get; protected set; }
    public virtual StoneClampRatio? StoneClampRatio { get; protected set; }

    private IList<NormFactorAssignmentCode> _normFactorAssignmentCodes = new List<NormFactorAssignmentCode>();
    public IReadOnlyList<NormFactorAssignmentCode> NormFactorAssignmentCodes => _normFactorAssignmentCodes.ToList();

    private IList<NormFactor> _childNormFactors = new List<NormFactor>();
    public IReadOnlyList<NormFactor> ChildNormFactors => _childNormFactors.ToList();


    public static NormFactor Create(Guid productionProcessId, Guid hardnessId, Guid stoneClampRatioId, double value, Guid? referenceNormFactorId = null)
    {
        return new NormFactor
        {
            ProductionProcessId = productionProcessId,
            HardnessId = hardnessId,
            StoneClampRatioId = stoneClampRatioId,
            Value = value,
            ReferenceNormFactorId = referenceNormFactorId
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

    public void Update(Guid productionProcessId, Guid hardnessId, Guid stoneClampRatioId, double value, Guid? referenceNormFactorId = null)
    {
        ProductionProcessId = productionProcessId;
        HardnessId = hardnessId;
        StoneClampRatioId = stoneClampRatioId;
        Value = value;
        ReferenceNormFactorId = referenceNormFactorId;
    }
}
