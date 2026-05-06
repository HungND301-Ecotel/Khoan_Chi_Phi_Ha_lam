using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;

namespace Domain.Entities.Index;

public class Code(string value) : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; set; } = value;

    public virtual AdjustmentFactor? AdjustmentFactor { get; protected set; }
    public virtual AssignmentCode? AssignmentCode { get; protected set; }
    public virtual Department? Department { get; protected set; }
    public virtual Equipment? Equipment { get; protected set; }
    public virtual Material? Material { get; protected set; }
    public virtual Part? Part { get; protected set; }
    public virtual ProcessGroup? ProcessGroup { get; protected set; }
    public virtual Product? Product { get; protected set; }
    public virtual ProductionProcess? ProductionProcess { get; protected set; }
    public virtual MaterialUnitPrice? MaterialUnitPrice { get; protected set; }
    public virtual SlideUnitPrice? SlideUnitPrice { get; protected set; }
    public virtual ProductionOrder? ProductionOrder { get; protected set; }

}
