using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;

namespace Domain.Entities.Index;

public class SeamFace : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }

    //Navigation Properties
    private IList<LongwallMaterialUnitPrice> _materialUnitPrices = new List<LongwallMaterialUnitPrice>();
    public virtual IReadOnlyCollection<LongwallMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

    public static SeamFace Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị mặt nứt không được để trống.");
        }

        return new SeamFace
        {
            Value = value
        };
    }

    public void Update(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị mặt nứt không được để trống.");
        }
        Value = value;
    }

    public bool CheckChange(SeamFace dto)
    {
        return !(Value == dto.Value);
    }
}
