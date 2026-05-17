namespace Commons.EventBus;

public interface IEventPublisher
{
    void Publish<TEvent>(TEvent @event)
        where TEvent : IEvent;
    void Publish<TEvent>(TEvent @event, string eventName)
        where TEvent : IEvent;
}
