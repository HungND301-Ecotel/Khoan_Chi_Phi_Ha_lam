using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class NormFactorAssignmentCode : AuditableEntity<Guid>
{
    public Guid NormFactorId { get; protected set; }
    public Guid AssignmentCodeId { get; protected set; }

    public virtual NormFactor NormFactor { get; protected set; }
    public virtual AssignmentCode AssignmentCode { get; protected set; }

    public static NormFactorAssignmentCode Create(Guid assignmentCodeId, Guid normFactorId)
    {
        return new NormFactorAssignmentCode
        {
            NormFactorId = normFactorId,
            AssignmentCodeId = assignmentCodeId
        };
    }
}
