using Application.Common.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Events;

namespace Infrastructure.Common.Services;

public class EventPublisher(ILogger<EventPublisher> logger, IPublisher mediator) : IEventPublisher
{
    public Task PublishAsync(IEvent @event)
    {
        logger.LogInformation("Publishing event : {Event}", @event.GetType().Name);
        return mediator.Publish(CreateEventNotification(@event));
    }

    private static INotification CreateEventNotification(IEvent @event) =>
        (INotification)Activator.CreateInstance(
            typeof(EventNotification<>).MakeGenericType(@event.GetType()), @event)!;
}