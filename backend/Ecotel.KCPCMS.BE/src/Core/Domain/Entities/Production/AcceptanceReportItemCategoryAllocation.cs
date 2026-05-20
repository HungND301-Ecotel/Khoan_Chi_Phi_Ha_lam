using Domain.Common.Contracts;
using Domain.Entities.Index;
using Shared.Constants;

namespace Domain.Entities.Production;

public class AcceptanceReportItemCategoryAllocation : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }
    public double Quantity { get; protected set; }

    public virtual AcceptanceReportItem AcceptanceReportItem { get; protected set; }
    public virtual ProcessGroup ProcessGroup { get; protected set; }

    private IList<AcceptanceReportItemCategoryAllocationEquipment> _equipments =
        new List<AcceptanceReportItemCategoryAllocationEquipment>();
    public virtual IReadOnlyCollection<AcceptanceReportItemCategoryAllocationEquipment> Equipments =>
        _equipments.AsReadOnly();
    public IEnumerable<Guid> AssignmentCodeIds => _equipments.Select(x => x.AssignmentCodeId);

    private IList<AcceptanceReportItemLog> _acceptanceReportItemLogs = new List<AcceptanceReportItemLog>();
    public virtual IReadOnlyCollection<AcceptanceReportItemLog> AcceptanceReportItemLogs =>
        _acceptanceReportItemLogs.AsReadOnly();

    public static AcceptanceReportItemCategoryAllocation Create(
        Guid acceptanceReportItemId,
        Guid processGroupId,
        double quantity,
        IList<Guid>? assignmentCodeIds)
    {
        if (processGroupId == Guid.Empty)
        {
            throw new ArgumentException(CustomResponseMessage.ProcessGroupNotFound);
        }

        if (quantity < 0)
        {
            throw new ArgumentException("Số lượng phân bổ theo nhóm công đoạn không được âm");
        }

        var allocation = new AcceptanceReportItemCategoryAllocation
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            ProcessGroupId = processGroupId,
            Quantity = quantity,
        };

        allocation.SyncAssignmentCodeIds(assignmentCodeIds);
        return allocation;
    }

    public void Update(double quantity, IList<Guid>? assignmentCodeIds)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Số lượng phân bổ theo nhóm công đoạn không được âm");
        }

        Quantity = quantity;
        SyncAssignmentCodeIds(assignmentCodeIds);
    }

    private void SyncAssignmentCodeIds(IList<Guid>? assignmentCodeIds)
    {
        _equipments.Clear();

        if (assignmentCodeIds == null)
        {
            return;
        }

        foreach (var assignmentCodeId in assignmentCodeIds.Where(id => id != Guid.Empty).Distinct())
        {
            _equipments.Add(AcceptanceReportItemCategoryAllocationEquipment.Create(Id, assignmentCodeId));
        }
    }
}
