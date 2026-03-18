using Domain.Common.Contracts;

namespace Domain.Events;

public class SyncUserInfoEvent(long userId) : DomainEvent
{
    public long UserId { get; private set; } = userId;
}