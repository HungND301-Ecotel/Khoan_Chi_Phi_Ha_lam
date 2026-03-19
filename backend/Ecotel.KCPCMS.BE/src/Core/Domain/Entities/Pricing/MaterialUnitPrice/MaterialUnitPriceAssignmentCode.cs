using Domain.Common.Contracts;
using Domain.Entities.Index;
using MaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice;

namespace Domain.Entities.Pricing.MaterialUnitPrice;

public class MaterialUnitPriceAssignmentCode : AuditableEntity<Guid>
{
    public Guid MaterialUnitPriceId { get; protected set; }
    public Guid AssignmentCodeId { get; protected set; }
    public double TotalPrice { get; set; }

    public virtual MaterialUnitPriceEntity MaterialUnitPrice { get; protected set; }
    public virtual AssignmentCode AssignmentCode { get; protected set; }
}
