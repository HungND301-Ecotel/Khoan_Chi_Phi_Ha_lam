using Domain.Common.Contracts;
using Domain.Entities.Pricing;

namespace Domain.Entities.Production;

public class AcceptanceReport : AuditableEntity<Guid>
{
    public Guid ProductionOutputId { get; protected set; }
    public string FilePath { get; protected set; }

    //Navigation Properties
    public virtual ProductionOutput? ProductionOutput { get; protected set; }
    public virtual ActualElectricityCost? ActualElectricityCost { get; protected set; }

    private IList<AcceptanceReportItem> _acceptanceReportItems = new List<AcceptanceReportItem>();
    public virtual IReadOnlyCollection<AcceptanceReportItem> AcceptanceReportItems => _acceptanceReportItems.AsReadOnly();

    private IList<AcceptanceReportItemLog> _acceptanceReportItemLogs = new List<AcceptanceReportItemLog>();
    public virtual IReadOnlyCollection<AcceptanceReportItemLog> AcceptanceReportItemLogs => _acceptanceReportItemLogs.AsReadOnly();

    private IList<LongTermAnchorSeedItemLog> _longTermAnchorSeedItemLogs = new List<LongTermAnchorSeedItemLog>();
    public virtual IReadOnlyCollection<LongTermAnchorSeedItemLog> LongTermAnchorSeedItemLogs => _longTermAnchorSeedItemLogs.AsReadOnly();

    public static AcceptanceReport Create(Guid productionOutputId, string filePath)
    {
        return new AcceptanceReport
        {
            ProductionOutputId = productionOutputId,
            FilePath = filePath
        };
    }

    public void Update(string filePath)
    {
        FilePath = filePath;
    }
}
