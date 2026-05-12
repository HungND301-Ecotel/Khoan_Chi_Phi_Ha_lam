using Domain.Common.Contracts;
using Domain.Entities.Pricing;
using Domain.Entities.Production;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Department : AuditableEntity<Guid>, IAggregateRoot
{
    public Guid CodeId { get; protected set; }
    public string Name { get; protected set; }

    // Navigation properties
    public virtual Code? Code { get; protected set; }

    private IList<ProductUnitPrice> _productUnitPrices = new List<ProductUnitPrice>();
    public virtual IReadOnlyCollection<ProductUnitPrice> ProductUnitPrices => _productUnitPrices.AsReadOnly();

    private IList<ProductionOutput> _productionOutputs = new List<ProductionOutput>();
    public virtual IReadOnlyCollection<ProductionOutput> ProductionOutputs => _productionOutputs.AsReadOnly();

    private IList<LongTermAnchorSeed> _longTermAnchorSeeds = new List<LongTermAnchorSeed>();
    public virtual IReadOnlyCollection<LongTermAnchorSeed> LongTermAnchorSeeds => _longTermAnchorSeeds.AsReadOnly();

    private IList<LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPrices = new List<LowValuePerishableSupplyUnitPrice>();
    public IReadOnlyList<LowValuePerishableSupplyUnitPrice> LowValuePerishableSupplyUnitPrices => _lowValuePerishableSupplyUnitPrices.ToList();

    public static Department Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new Department
        {
            Code = new Code(code.ToUpper()),
            Name = name
        };
    }

    public static Department Create(Guid id, string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new Department
        {
            Id = id,
            Code = new Code(code.ToUpper()),
            Name = name
        };
    }

    public void Update(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        if (Code != null)
        {
            Code.Value = code.ToUpper();
        }

        Name = name;
    }

    public bool CheckChange(Department dto)
    {
        return !(Code?.Value == dto.Code?.Value && Name == dto.Name);
    }
}
