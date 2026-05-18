namespace Commons.EventBus;

public interface IEventPublisher
{
    void Publish(IEvent @event);
    void Publish(IEvent @event, string eventName);
}
