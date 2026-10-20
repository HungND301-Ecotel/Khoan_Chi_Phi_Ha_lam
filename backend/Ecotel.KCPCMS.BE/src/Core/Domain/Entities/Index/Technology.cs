using Domain.Common.Contracts;
using Domain.Entities.Pricing.MaterialUnitPrice;

namespace Domain.Entities.Index;

public class Technology : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }
    public string? Note { get; protected set; }

    //Navigation Properties
    private IList<MaterialUnitPrice> _materialUnitPrices = new List<MaterialUnitPrice>();
    public virtual IReadOnlyCollection<MaterialUnitPrice> MaterialUnitPrices => _materialUnitPrices.AsReadOnly();

    public static Technology Create(string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị công nghệ không được để trống.");
        }

        return new Technology
        {
            Value = value,
            Note = NormalizeNote(note)
        };
    }

    public void Update(string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Giá trị công nghệ không được để trống.");
        }
        Value = value;
        Note = NormalizeNote(note);
    }

    public bool CheckChange(Technology dto)
    {
        return !(Value == dto.Value
            && Note == NormalizeNote(dto.Note));
    }

    private static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
