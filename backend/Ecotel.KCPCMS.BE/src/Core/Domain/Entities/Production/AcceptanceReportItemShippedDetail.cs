using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.MasterData;

namespace Domain.Entities.Production;

public class AcceptanceReportItemShippedDetail : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemId { get; protected set; }
    public Guid? FixedKeyId { get; protected set; }
    public ShippedQuantityType Type { get; protected set; }
    public double Quantity { get; protected set; }

    public virtual AcceptanceReportItem? AcceptanceReportItem { get; protected set; }
    public virtual FixedKey? FixedKey { get; protected set; }

    public static AcceptanceReportItemShippedDetail Create(
        Guid acceptanceReportItemId,
        ShippedQuantityType type,
        double quantity,
        Guid? fixedKeyId = null)
    {
        return new AcceptanceReportItemShippedDetail
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            FixedKeyId = fixedKeyId,
            Type = type,
            Quantity = quantity
        };
    }
}
