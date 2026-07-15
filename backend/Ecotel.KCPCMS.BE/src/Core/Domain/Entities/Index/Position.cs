using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common.Contracts;
using Domain.Common.Events;
using Shared.Constants;

namespace Domain.Entities.Index;

public class Position : AuditableEntity<int>, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public int? Level { get; private set;  }
    public string Description { get;private set; }
    public bool IsActive { get; private set; } = true;

    private readonly IList<Employee> _employees = new List<Employee>();
    public virtual IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    protected Position() { }

    public static Position Create(string name, int level,string description)
    {
        if(string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        if (string.IsNullOrEmpty(description))
        {
            throw new ArgumentException(CustomResponseMessage.DescriptionCannotBeNullOrEmpty);
        }

        return new Position
        {
            Name = name,
            Level = level,
            Description= description,
            IsActive = true
        };
    }

    public void Update(string name,int level,string description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }
        SetName(name);
        SetActiveStatus(isActive);
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    #region Constructors

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(CustomResponseMessage.NameCannotBeNullOrEmpty);
        }

        Name = name;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }

    public void SetActiveStatus(bool isActive)
    {
        IsActive = isActive;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
    public void SetLevel(int level)
    {
        Level = level;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
    public void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(CustomResponseMessage.DescriptionCannotBeNullOrEmpty);
        }
        Description = description;
        DomainEvents.Add(EntityUpdatedEvent.WithEntity(this));
    }
    #endregion
}