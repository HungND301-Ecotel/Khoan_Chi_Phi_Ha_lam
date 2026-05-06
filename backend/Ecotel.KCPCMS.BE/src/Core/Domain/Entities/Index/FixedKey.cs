using Domain.Common.Contracts;
using Domain.Common.Enums;
using Shared.Constants;

namespace Domain.Entities.Index;

public class FixedKey : AuditableEntity<Guid>, IAggregateRoot
{
    public string Key { get; protected set; }
    public string Name { get; protected set; }
    public FixedKeyType Type { get; protected set; }

    private IList<ProcessGroup> _processGroups = new List<ProcessGroup>();
    public virtual IReadOnlyCollection<ProcessGroup> ProcessGroups => _processGroups.AsReadOnly();

    private IList<AdjustmentFactor> _adjustmentFactors = new List<AdjustmentFactor>();
    public virtual IReadOnlyCollection<AdjustmentFactor> AdjustmentFactors => _adjustmentFactors.AsReadOnly();

    public static FixedKey Create(string key, string name, FixedKeyType type)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new FixedKey
        {
            Key = key.ToUpper(),
            Name = name,
            Type = type,
        };
    }

    public static FixedKey Create(Guid id, string key, string name, FixedKeyType type)
    {
        var fixedKey = Create(key, name, type);
        fixedKey.Id = id;
        return fixedKey;
    }

    public void Update(string key, string name, FixedKeyType type)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(CustomResponseMessage.CodeCannotBeNullOrEmpty);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        Key = key.ToUpper();
        Name = name;
        Type = type;
    }

    public bool CheckChange(FixedKey dto)
    {
        return !(Key == dto.Key && Name == dto.Name && Type == dto.Type);
    }
}