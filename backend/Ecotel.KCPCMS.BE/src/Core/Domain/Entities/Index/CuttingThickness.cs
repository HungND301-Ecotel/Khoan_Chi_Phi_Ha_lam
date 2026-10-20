using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Shared.Constants;

namespace Domain.Entities.Index;

public class CuttingThickness : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }
    public string? Note { get; protected set; }

    //Navigation Properties
    private IList<LongwallMaterialUnitPrice> _materialUnitPrices = new List<LongwallMaterialUnitPrice>();
    public virtual IReadOnlyCollection<LongwallMaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

    public static CuttingThickness Create(string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new CuttingThickness
        {
            Value = value,
            Note = NormalizeNote(note)
        };
    }

    public static CuttingThickness Create(Guid id, string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new CuttingThickness
        {
            Id = id,
            Value = value,
            Note = NormalizeNote(note)
        };
    }

    public void Update(string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        Value = value;
        Note = NormalizeNote(note);
    }

    public bool CheckChange(CuttingThickness dto)
    {
        return !(Value == dto.Value && Note == NormalizeNote(dto.Note));
    }

    private static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
