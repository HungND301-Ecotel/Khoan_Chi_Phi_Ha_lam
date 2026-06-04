using Domain.Common.Contracts;
using Domain.Entities.Index;
using MaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice;

namespace Domain.Entities.Pricing.MaterialUnitPrice;

public class MaterialUnitPriceAssignmentCode : AuditableEntity<Guid>
{
    public Guid MaterialUnitPriceId { get; protected set; }
    public Guid AssignmentCodeId { get; protected set; }
    public Guid? MaterialId { get; protected set; }
    public double Norm { get; protected set; }
    public double TotalPrice { get; protected set; }

    public virtual MaterialUnitPriceEntity MaterialUnitPrice { get; protected set; }
    public virtual AssignmentCode AssignmentCode { get; protected set; }
    public virtual Material? Material { get; protected set; }

    public static MaterialUnitPriceAssignmentCode Create(
        Guid assignmentCodeId,
        double totalPrice,
        Guid? materialId = null,
        double norm = 0)
    {
        return new MaterialUnitPriceAssignmentCode
        {
            AssignmentCodeId = assignmentCodeId,
            MaterialId = materialId,
            Norm = norm,
            TotalPrice = totalPrice
        };
    }
}
