using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class EquipmentProcessGroup : AuditableEntity<Guid>
{
    public Guid EquipmentId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }

    public virtual Equipment Equipment { get; protected set; }
    public virtual ProcessGroup ProcessGroup { get; protected set; }

    public static EquipmentProcessGroup Create(Guid equipmentId, Guid processGroupId)
    {
        return new EquipmentProcessGroup
        {
            EquipmentId = equipmentId,
            ProcessGroupId = processGroupId
        };
    }
}
