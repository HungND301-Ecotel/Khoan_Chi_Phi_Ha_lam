using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class NormFactorAssignmentCode : AuditableEntity<Guid>
{
    public Guid NormFactorId { get; protected set; }
    public Guid AssignmentCodeId { get; protected set; }
    public double Value { get; protected set; }
    public Guid? TargetHardnessId { get; protected set; }

    public virtual NormFactor NormFactor { get; protected set; }
    public virtual Hardness? TargetHardness { get; protected set; }
    public virtual AssignmentCode AssignmentCode { get; protected set; }

    public static NormFactorAssignmentCode Create(Guid assignmentCodeId, Guid normFactorId, double value, Guid? targetHardnessId)
    {
        return new NormFactorAssignmentCode
        {
            NormFactorId = normFactorId,
            AssignmentCodeId = assignmentCodeId,
            Value = value,
            TargetHardnessId = targetHardnessId
        };
    }

    public void Update(double value, Guid? targetHardnessId)
    {
        Value = value;
        TargetHardnessId = targetHardnessId;
    }
}
