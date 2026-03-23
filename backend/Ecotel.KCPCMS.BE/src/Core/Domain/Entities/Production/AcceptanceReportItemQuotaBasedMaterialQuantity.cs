using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Production;

public class AcceptanceReportItemQuotaBasedMaterialQuantity : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemId { get; protected set; }
    public QuotaBasedMaterialType Type { get; protected set; }
    public double Quantity { get; protected set; }

    //Navigation Properties
    public virtual AcceptanceReportItem? AcceptanceReportItem { get; protected set; }

    public static AcceptanceReportItemQuotaBasedMaterialQuantity Create(Guid acceptanceReportItemId, QuotaBasedMaterialType type, double quantity)
    {
        return new AcceptanceReportItemQuotaBasedMaterialQuantity
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            Type = type,
            Quantity = quantity
        };
    }

}
