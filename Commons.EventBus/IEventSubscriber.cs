namespace Commons.EventBus;

public interface IEventSubscriber
{
    void Subscribe<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;
    void Subscribe(Type eventType, Type eventHandler);

    void Subscribe<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;
    void Subscribe(Type eventType, Type eventHandler, string eventName);

    void Unsubscribe<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;
    void Unsubscribe(Type eventType, Type eventHandler);

    void Unsubscribe<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;
    void Unsubscribe(Type eventType, Type eventHandler, string eventName);
}
