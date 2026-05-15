using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class AssignmentCodeMaterial : AuditableEntity<Guid>
{
    public Guid AssignmentCodeId { get; protected set; }
    public Guid MaterialId { get; protected set; }

    public virtual AssignmentCode AssignmentCode { get; protected set; }
    public virtual Material Material { get; protected set; }

    public static AssignmentCodeMaterial Create(Guid assignmentCodeId, Guid materialId)
    {
        return new AssignmentCodeMaterial
        {
            AssignmentCodeId = assignmentCodeId,
            MaterialId = materialId,
        };
    }

    public static AssignmentCodeMaterial Create(AssignmentCode assignmentCode, Material material)
    {
        ArgumentNullException.ThrowIfNull(assignmentCode);
        ArgumentNullException.ThrowIfNull(material);

        return new AssignmentCodeMaterial
        {
            AssignmentCodeId = assignmentCode.Id,
            MaterialId = material.Id,
            AssignmentCode = assignmentCode,
            Material = material,
        };
    }
}
