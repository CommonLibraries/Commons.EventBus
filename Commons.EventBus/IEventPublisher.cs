using Commons.EventBus.Events;

namespace Commons.EventBus;

public interface IEventPublisher
{
    void Publish(IEvent @event);
    void Publish(IEvent @event, string eventName);
    Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default);
    Task PublishAsync(IEvent @event, string eventName, CancellationToken cancellationToken = default);
}
