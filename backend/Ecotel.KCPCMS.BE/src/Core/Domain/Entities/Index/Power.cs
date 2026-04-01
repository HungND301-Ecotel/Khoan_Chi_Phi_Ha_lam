using Domain.Common.Contracts;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Power : AuditableEntity<Guid>, IAggregateRoot
{
    public string Value { get; protected set; }


    public static Power Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.HardnessValueNullOrEmpty);
        }

        return new Power
        {
            Value = value
        };
    }

    public void Update(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(CustomResponseMessage.HardnessValueNullOrEmpty);
        }
        Value = value;
    }

    public bool CheckChange(Power dto)
    {
        return !(Value == dto.Value);
    }
}
