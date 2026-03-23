using Domain.Common.Contracts;
using Domain.Entities.Production;
using Shared.Constants;

namespace Domain.Entities.Index;

public class ProductionOrder : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public string Name { get; protected set; }
    public DateOnly StartMonth { get; protected set; }
    public DateOnly EndMonth { get; protected set; }

    //Navigation Properties
    public virtual Code Code { get; protected set; }

    private IList<AcceptanceReportItem> _acceptanceReportItems = new List<AcceptanceReportItem>();
    public virtual IReadOnlyCollection<AcceptanceReportItem> AcceptanceReportItems => _acceptanceReportItems.AsReadOnly();

    public static ProductionOrder Create(string code, string name, DateOnly startMonth, DateOnly endMonth)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.ProductionOrderValueNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartDateMustBeEarlierThanEndDate);
        }

        return new ProductionOrder
        {
            Code = new Code(code.ToUpper()),
            Name = name,
            StartMonth = startMonth,
            EndMonth = endMonth
        };
    }

    public void Update(string code, string name, DateOnly startMonth, DateOnly endMonth)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.ProductionOrderValueNullOrEmpty);
        }

        if (startMonth > endMonth)
        {
            throw new ArgumentException(CustomResponseMessage.StartDateMustBeEarlierThanEndDate);
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (Code != null)
        {
            Code.Value = code.ToUpper();
        }

        Name = name;
        StartMonth = startMonth;
        EndMonth = endMonth;
    }

    public bool CheckChange(ProductionOrder dto)
    {
        return !(Code?.Value == dto.Code?.Value && Name == dto.Name && StartMonth == dto.StartMonth && EndMonth == dto.EndMonth);
    }
}
