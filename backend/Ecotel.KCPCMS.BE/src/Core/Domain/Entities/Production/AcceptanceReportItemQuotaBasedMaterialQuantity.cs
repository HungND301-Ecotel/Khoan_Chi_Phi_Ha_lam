using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.MasterData;

namespace Domain.Entities.Production;

public class AcceptanceReportItemQuotaBasedMaterialQuantity : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemId { get; protected set; }
    public Guid? FixedKeyId { get; protected set; }
    public QuotaBasedMaterialType Type { get; protected set; }
    public double Quantity { get; protected set; }

    //Navigation Properties
    public virtual AcceptanceReportItem? AcceptanceReportItem { get; protected set; }
    public virtual FixedKey? FixedKey { get; protected set; }

    public static AcceptanceReportItemQuotaBasedMaterialQuantity Create(
        Guid acceptanceReportItemId,
        QuotaBasedMaterialType type,
        double quantity,
        Guid? fixedKeyId = null)
    {
        return new AcceptanceReportItemQuotaBasedMaterialQuantity
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            FixedKeyId = fixedKeyId,
            Type = type,
            Quantity = quantity
        };
    }

}
