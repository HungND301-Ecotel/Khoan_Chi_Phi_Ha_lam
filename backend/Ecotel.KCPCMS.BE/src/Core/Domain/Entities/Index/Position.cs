using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Common.Events;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Position : AuditableEntity<int>, IAggregateRoot
{
    [MaxLength(50)]
    public string? Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int? Level { get; private set;  }

    // Hiển thị trên giao diện là "Ghi chú"
    public string? Description { get; private set; }

    private readonly IList<Employee> _employees = new List<Employee>();
    public virtual IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    protected Position() { }

    public static Position Create(string name, int level, string? description, string? code = null)
    {
        if(string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        return new Position
        {
            Code = NormalizeCode(code),
            Name = name,
            Level = level,
            Description = NormalizeText(description)
        };
    }

    public void Update(string name, int level, string? description, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        SetName(name);
        SetLevel(level);
        SetDescription(description);
        SetCode(code);
    }

    #region Constructors

    public void SetCode(string? code)
    {
        Code = NormalizeCode(code);
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        Name = name;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
    public void SetLevel(int level)
    {
        Level = level;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
    public void SetDescription(string? description)
    {
        Description = NormalizeText(description);
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    private static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpper();

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    #endregion
}
