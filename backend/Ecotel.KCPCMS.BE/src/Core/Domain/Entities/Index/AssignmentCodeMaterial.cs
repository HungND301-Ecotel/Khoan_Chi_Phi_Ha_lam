using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Index;

public class AssignmentCodeMaterial : AuditableEntity<Guid>
{
    public Guid AssignmentCodeId { get; protected set; }
    public Guid MaterialId { get; protected set; }
    public AssignmentCodeMaterialRole Role { get; protected set; } = AssignmentCodeMaterialRole.Material;

    public virtual AssignmentCode AssignmentCode { get; protected set; }
    public virtual Material Material { get; protected set; }

    public static AssignmentCodeMaterial Create(
        Guid assignmentCodeId,
        Guid materialId,
        AssignmentCodeMaterialRole role = AssignmentCodeMaterialRole.Material)
    {
        return new AssignmentCodeMaterial
        {
            AssignmentCodeId = assignmentCodeId,
            MaterialId = materialId,
            Role = role,
        };
    }

    public static AssignmentCodeMaterial Create(
        AssignmentCode assignmentCode,
        Material material,
        AssignmentCodeMaterialRole role = AssignmentCodeMaterialRole.Material)
    {
        ArgumentNullException.ThrowIfNull(assignmentCode);
        ArgumentNullException.ThrowIfNull(material);

        return new AssignmentCodeMaterial
        {
            AssignmentCodeId = assignmentCode.Id,
            MaterialId = material.Id,
            Role = role,
            AssignmentCode = assignmentCode,
            Material = material,
        };
    }
}
