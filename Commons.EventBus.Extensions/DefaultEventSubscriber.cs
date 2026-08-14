using Commons.EventBus.Events;

namespace Commons.EventBus.Extensions;

internal class DefaultEventSubscriber : IEventSubscriber
{
    private readonly IEventBus eventBus;

    public DefaultEventSubscriber(IEventBus eventBus)
    {
        this.eventBus = eventBus;
    }

    public void Subscribe<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.eventBus.Subscribe<TEvent, TEventHandler>();
    }

    public void Subscribe(Type eventType, Type eventHandler)
    {
        this.eventBus.Subscribe(eventType, eventHandler);
    }

    public void Subscribe<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.eventBus.Subscribe<TEvent, TEventHandler>(eventName);
    }

    public void Subscribe(Type eventType, Type eventHandler, string eventName)
    {
        this.Subscribe(eventType, eventHandler, eventName);
    }

    public void Unsubscribe<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.Unsubscribe<TEvent, TEventHandler>();
    }

    public void Unsubscribe(Type eventType, Type eventHandler)
    {
        this.Unsubscribe(eventType, eventHandler);
    }

    public void Unsubscribe<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.Unsubscribe<TEvent, TEventHandler>(eventName);
    }

    public void Unsubscribe(Type eventType, Type eventHandler, string eventName)
    {
        this.Unsubscribe(eventType, eventHandler, eventName);
    }
}
