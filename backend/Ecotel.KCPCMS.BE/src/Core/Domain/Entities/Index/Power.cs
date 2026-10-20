using Domain.Common.Contracts;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Power : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }
    public string? Note { get; protected set; }


    public static Power Create(string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.HardnessValueNullOrEmpty);
        }

        return new Power
        {
            Value = value,
            Note = NormalizeNote(note)
        };
    }

    public void Update(string value, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.HardnessValueNullOrEmpty);
        }
        Value = value;
        Note = NormalizeNote(note);
    }

    public bool CheckChange(Power dto)
    {
        return !(Value == dto.Value
            && Note == NormalizeNote(dto.Note));
    }

    private static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
