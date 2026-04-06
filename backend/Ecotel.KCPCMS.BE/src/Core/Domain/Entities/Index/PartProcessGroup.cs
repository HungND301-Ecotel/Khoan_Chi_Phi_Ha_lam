using Domain.Common.Contracts;

namespace Domain.Entities.Index;

public class PartProcessGroup : AuditableEntity<Guid>
{
    public Guid PartId { get; protected set; }
    public Guid ProcessGroupId { get; protected set; }

    public virtual Part Part { get; protected set; }
    public virtual ProcessGroup ProcessGroup { get; protected set; }

    public static PartProcessGroup Create(Guid partId, Guid processGroupId)
    {
        return new PartProcessGroup
        {
            PartId = partId,
            ProcessGroupId = processGroupId
        };
    }
}
