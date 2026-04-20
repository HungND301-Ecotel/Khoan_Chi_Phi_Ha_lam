using Domain.Common.Contracts;
using Domain.Common.Enums;
using Domain.Entities.Index;

namespace Domain.Entities.MasterData;

public class FixedKey : AuditableEntity<Guid>, IAggregateRoot
{
    public string Code { get; protected set; }
    public string Name { get; protected set; }
    public FixedKeyType Type { get; protected set; }
    public bool IsSystem { get; protected set; } = true;

    private IList<ProcessGroup> _processGroups = new List<ProcessGroup>();
    public IReadOnlyCollection<ProcessGroup> ProcessGroups => _processGroups.AsReadOnly();

    public static FixedKey Create(string code, string name, FixedKeyType type, bool isSystem = true)
    {
        Validate(code, name, type);

        return new FixedKey
        {
            Code = code.Trim(),
            Name = name.Trim(),
            Type = type,
            IsSystem = isSystem,
        };
    }

    public void Update(string code, string name)
    {
        Validate(code, name, Type);

        Code = code.Trim();
        Name = name.Trim();
    }

    private static void Validate(string code, string name, FixedKeyType type)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Fixed key code cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Fixed key name cannot be empty.");
        }

        if (type == FixedKeyType.None)
        {
            throw new ArgumentException("Fixed key type is required.");
        }
    }
}