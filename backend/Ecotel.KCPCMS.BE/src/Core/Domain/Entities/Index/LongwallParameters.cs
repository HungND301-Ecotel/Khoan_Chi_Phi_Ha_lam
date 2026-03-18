using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index;

public class LongwallParameters : AuditableEntity<Guid>, IAggregateRoot
{
    public string Llc { get; protected set; }
    public string Lkc { get; protected set; }
    public string Mk { get; protected set; }

    //Navigation Properties
    private IList<LongwallMaterialUnitPrice> _materialUnitPrices = new List<LongwallMaterialUnitPrice>();
    public virtual IReadOnlyCollection<LongwallMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

    // Constructor
    public static LongwallParameters Create(string llc, string lkc, string mk)
    {
        if (string.IsNullOrWhiteSpace(llc) || string.IsNullOrWhiteSpace(lkc) || string.IsNullOrWhiteSpace(mk))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new LongwallParameters
        {
            Llc = llc,
            Lkc = lkc,
            Mk = mk
        };
    }

    public static LongwallParameters Create(Guid id, string llc, string lkc, string mk)
    {
        if (string.IsNullOrWhiteSpace(llc) || string.IsNullOrWhiteSpace(lkc) || string.IsNullOrWhiteSpace(mk))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new LongwallParameters
        {
            Id = id,
            Llc = llc,
            Lkc = lkc,
            Mk = mk
        };
    }

    public void Update(string llc, string lkc, string mk)
    {
        if (string.IsNullOrWhiteSpace(llc) || string.IsNullOrWhiteSpace(lkc) || string.IsNullOrWhiteSpace(mk))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        Llc = llc;
        Lkc = lkc;
        Mk = mk;
    }

    public bool CheckChange(LongwallParameters dto)
    {
        return !(Llc == dto.Llc && Lkc == dto.Lkc && Mk == dto.Mk);
    }
}
