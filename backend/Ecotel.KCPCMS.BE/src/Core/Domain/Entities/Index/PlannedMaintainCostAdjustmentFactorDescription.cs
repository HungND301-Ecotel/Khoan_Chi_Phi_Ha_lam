using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index;
public class PlannedMaintainCostAdjustmentFactorDescription : AuditableEntity<Guid>
{
    public Guid PlannedMaintainCostAdjustmentFactorId { get; set; }
    public Guid AdjustmentFactorDescriptionId { get; set; }
    //Navigation Properties
    public virtual PlannedMaintainCostAdjustmentFactor? PlannedMaintainCostAdjustmentFactor { get; set; }
    public virtual AdjustmentFactorDescription? AdjustmentFactorDescription { get; set; }
}
