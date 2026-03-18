using Domain.Common.Contracts;
using Domain.Entities.Identity;

namespace Domain.Events;

public class UserEmailVerifiedEvent : DomainEvent
{
    public UserEmailVerifiedEvent(User user)
    {
        User = user;
    }

    public User User { get; }
}

public class UserPhoneVerifiedEvent : DomainEvent
{
    public UserPhoneVerifiedEvent(User user)
    {
        User = user;
    }

    public User User { get; }
}

public class UserLockedOutEvent : DomainEvent
{
    public UserLockedOutEvent(User user, TimeSpan duration)
    {
        User = user;
        LockoutDuration = duration;
    }

    public User User { get; }
    public TimeSpan LockoutDuration { get; }
}

public class UserPasswordChangedEvent : DomainEvent
{
    public UserPasswordChangedEvent(User user)
    {
        User = user;
    }

    public User User { get; }
}