using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;

namespace Domain.Entities.Index;

public class Technology : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }

    //Navigation Properties
    private IList<MaterialUnitPrice> _materialUnitPrices = new List<MaterialUnitPrice>();
    public virtual IReadOnlyCollection<MaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

    public static Technology Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị công nghệ không được để trống.");
        }

        return new Technology
        {
            Value = value
        };
    }

    public void Update(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị công nghệ không được để trống.");
        }
        Value = value;
    }

    public bool CheckChange(Technology dto)
    {
        return !(Value == dto.Value);
    }
}
