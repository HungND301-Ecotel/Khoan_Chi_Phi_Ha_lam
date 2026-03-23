using Domain.Common.Contracts;
using Domain.Common.Enums;

namespace Domain.Entities.Production;

public class AcceptanceReportItemIssuedDetail : AuditableEntity<Guid>
{
    public Guid AcceptanceReportItemId { get; protected set; }
    public IssuedQuantityType Type { get; protected set; }
    public double Quantity { get; protected set; }

    //Navigation Properties
    public virtual AcceptanceReportItem? AcceptanceReportItem { get; protected set; }

    public static AcceptanceReportItemIssuedDetail Create(Guid acceptanceReportItemId, IssuedQuantityType type, double quantity)
    {
        return new AcceptanceReportItemIssuedDetail
        {
            AcceptanceReportItemId = acceptanceReportItemId,
            Type = type,
            Quantity = quantity
        };
    }

}
