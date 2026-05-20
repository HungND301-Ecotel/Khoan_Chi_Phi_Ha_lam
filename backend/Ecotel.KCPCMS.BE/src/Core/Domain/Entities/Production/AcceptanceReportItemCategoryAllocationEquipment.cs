using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class AcceptanceReportItemCategoryAllocationEquipment : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemCategoryAllocationId { get; protected set; }
    public Guid EquipmentId { get; protected set; }
    public Guid AssignmentCodeId => EquipmentId;

    public virtual AcceptanceReportItemCategoryAllocation AcceptanceReportItemCategoryAllocation { get; protected set; }
    public virtual Equipment Equipment { get; protected set; }

    public static AcceptanceReportItemCategoryAllocationEquipment Create(
        Guid acceptanceReportItemCategoryAllocationId,
        Guid assignmentCodeId)
    {
        return new AcceptanceReportItemCategoryAllocationEquipment
        {
            AcceptanceReportItemCategoryAllocationId = acceptanceReportItemCategoryAllocationId,
            EquipmentId = assignmentCodeId,
        };
    }
}
