using Domain.Common.Contracts;
using Domain.Entities.Production;

namespace Domain.Entities.Pricing;

public class ActualElectricityCost : AuditableEntity<Guid>
{
    public Guid AcceptanceReportId { get; protected set; }

    private double? CachedActualElectricityTotal { get; set; }

    //NavigationProperty
    public AcceptanceReport? AcceptanceReport { get; protected set; }

    private IList<ActualEletricityEquipment> _actualEletricityEquipment = new List<ActualEletricityEquipment>();
    public virtual IReadOnlyCollection<ActualEletricityEquipment> ActualEletricityEquipment => _actualEletricityEquipment.AsReadOnly();

    //Constructor
    public double GetActualTotalPrice()
    {
        if (CachedActualElectricityTotal.HasValue)
        {
            return CachedActualElectricityTotal.Value;
        }

        CachedActualElectricityTotal = _actualEletricityEquipment.Sum(p => p.GetCurrentElectricityCost(AcceptanceReport?.ProductionOutput?.StartMonth ?? DateOnly.FromDateTime(DateTime.Now)));
        return CachedActualElectricityTotal.Value;
    }

    public static ActualElectricityCost Create(Guid acceptanceReportId, IEnumerable<ActualEletricityEquipment> list)
    {
        var result = new ActualElectricityCost
        {
            AcceptanceReportId = acceptanceReportId,
        };
        result.AddActualEletricityEquipments(list.ToList());
        return result;
    }

    public void Update(Guid acceptanceReportId)
    {
        AcceptanceReportId = acceptanceReportId;
    }

    public void ClearActualEletricityEquipments()
    {
        _actualEletricityEquipment.Clear();
    }

    public void AddActualEletricityEquipments(IList<ActualEletricityEquipment> list)
    {
        foreach (var item in list)
        {
            _actualEletricityEquipment.Add(item);
        }
    }
}
