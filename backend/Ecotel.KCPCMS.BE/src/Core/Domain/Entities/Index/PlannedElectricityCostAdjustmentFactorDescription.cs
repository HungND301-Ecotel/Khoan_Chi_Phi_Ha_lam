using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Index;
public class PlannedElectricityCostAdjustmentFactorDescription : AuditableEntity<Guid>
{
    public Guid PlannedElectricityCostAdjustmentFactorId { get; set; }
    public Guid AdjustmentFactorDescriptionId { get; set; }
    //Navigation Properties
    public virtual PlannedElectricityCostAdjustmentFactor? PlannedElectricityCostAdjustmentFactor { get; set; }
    public virtual AdjustmentFactorDescription? AdjustmentFactorDescription { get; set; }
}
