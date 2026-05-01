using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class AcceptanceReportItemCategoryAllocationEquipment : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemCategoryAllocationId { get; protected set; }
    public Guid EquipmentId { get; protected set; }

    public virtual AcceptanceReportItemCategoryAllocation AcceptanceReportItemCategoryAllocation { get; protected set; }
    public virtual Equipment Equipment { get; protected set; }

    public static AcceptanceReportItemCategoryAllocationEquipment Create(
        Guid acceptanceReportItemCategoryAllocationId,
        Guid equipmentId)
    {
        return new AcceptanceReportItemCategoryAllocationEquipment
        {
            AcceptanceReportItemCategoryAllocationId = acceptanceReportItemCategoryAllocationId,
            EquipmentId = equipmentId,
        };
    }
}