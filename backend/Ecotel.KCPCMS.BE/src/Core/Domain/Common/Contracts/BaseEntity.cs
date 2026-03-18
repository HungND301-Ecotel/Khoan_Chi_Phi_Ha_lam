using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MassTransit;

namespace Domain.Common.Contracts;

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; protected set; } = default!;

    [NotMapped]
    [JsonIgnore]
    public List<DomainEvent> DomainEvents { get; } = [];
}