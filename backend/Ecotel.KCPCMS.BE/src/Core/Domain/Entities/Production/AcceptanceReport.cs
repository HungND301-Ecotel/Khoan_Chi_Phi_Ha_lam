using Domain.Common.Contracts;

namespace Domain.Entities.Production;

public class AcceptanceReport : AuditableEntity<Guid>
{
    public Guid ProductionOutputId { get; protected set; }
    public string FilePath { get; protected set; }

    //Navigation Properties
    public virtual ProductionOutput ProductionOutput { get; protected set; }

    private IList<AcceptanceReportItem> _acceptanceReportItems = new List<AcceptanceReportItem>();
    public virtual IReadOnlyCollection<AcceptanceReportItem> AcceptanceReportItems => _acceptanceReportItems.AsReadOnly();

    private IList<AcceptanceReportItemLog> _acceptanceReportItemLogs = new List<AcceptanceReportItemLog>();
    public virtual IReadOnlyCollection<AcceptanceReportItemLog> AcceptanceReportItemLogs => _acceptanceReportItemLogs.AsReadOnly();

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
