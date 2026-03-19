using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class EquipmentPart : AuditableEntity<Guid>
{
    public Guid EquipmentId { get; protected set; }
    public Guid PartId { get; protected set; }

    public virtual Equipment Equipment { get; protected set; }
    public virtual Part Part { get; protected set; }

    public static EquipmentPart Create(Guid equipmentId, Guid partId)
    {
        return new EquipmentPart
        {
            EquipmentId = equipmentId,
            PartId = partId,
        };
    }

    public static EquipmentPart Create(Equipment equipment, Part part)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(part);

        return new EquipmentPart
        {
            EquipmentId = equipment.Id,
            PartId = part.Id,
            Equipment = equipment,
            Part = part,
        };
    }
}
