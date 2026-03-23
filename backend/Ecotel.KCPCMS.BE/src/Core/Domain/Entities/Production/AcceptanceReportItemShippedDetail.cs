using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Production;

public class AcceptanceReportItemShippedDetail : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemId { get; protected set; }
    public ShippedQuantityType Type { get; protected set; }
    public double Quantity { get; protected set; }

    public virtual AcceptanceReportItem? AcceptanceReportItem { get; protected set; }

    public static AcceptanceReportItemShippedDetail Create(Guid acceptanceReportItemId, ShippedQuantityType type, double quantity)
    {
        return new AcceptanceReportItemShippedDetail
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            Type = type,
            Quantity = quantity
        };
    }
}
