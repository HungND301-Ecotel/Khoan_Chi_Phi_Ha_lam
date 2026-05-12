using Domain.Common.Contracts;
using Domain.Entities.Index;

namespace Domain.Entities.Production;

public class LongTermAnchorSeed : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid DepartmentId { get; protected set; }

    public virtual Department Department { get; protected set; } = default!;

    private readonly IList<LongTermAnchorSeedItem> _items = new List<LongTermAnchorSeedItem>();
    public virtual IReadOnlyCollection<LongTermAnchorSeedItem> Items => _items.AsReadOnly();

    private readonly IList<LongTermAnchorSeedProcessGroupMetric> _processGroupMetrics = new List<LongTermAnchorSeedProcessGroupMetric>();
    public virtual IReadOnlyCollection<LongTermAnchorSeedProcessGroupMetric> ProcessGroupMetrics => _processGroupMetrics.AsReadOnly();

    public static LongTermAnchorSeed Create(Guid departmentId)
    {
        return new LongTermAnchorSeed
        {
            DepartmentId = departmentId
        };
    }
}
