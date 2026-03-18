using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index;

public class CuttingThickness : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }

    //Navigation Properties
    private IList<LongwallMaterialUnitPrice> _materialUnitPrices = new List<LongwallMaterialUnitPrice>();
    public virtual IReadOnlyCollection<LongwallMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

    public static CuttingThickness Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new CuttingThickness
        {
            Value = value
        };
    }

    public static CuttingThickness Create(Guid id, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new CuttingThickness
        {
            Id = id,
            Value = value
        };
    }

    public void Update(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        Value = value;
    }

    public bool CheckChange(CuttingThickness dto)
    {
        return !(Value == dto.Value);
    }
}
