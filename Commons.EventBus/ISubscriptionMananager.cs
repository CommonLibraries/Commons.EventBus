using System;

namespace Commons.EventBus;

public interface ISubscriptionMananager
{
    bool IsEmpty { get; }

    string GetEventName<TEvent>()
        where TEvent : IEvent;
    string GetEventName(Type eventType);

    Type GetEventType(string eventName);

    bool HasSubscriptionsForEvent<TEvent>()
        where TEvent : IEvent;

    bool HasSubscriptionsForEvent(string eventName);

    IEnumerable<Subscription> GetSubscriptionsForEvent<TEvent>()
        where TEvent : IEvent;
    IEnumerable<Subscription> GetSubscriptionsForEvent(Type eventType);
    IEnumerable<Subscription> GetSubscriptionsForEvent(string eventName);

    event EventHandler<SubscriptionAddedArgs> OnEventAdded;
    event EventHandler<SubscriptionRemovedArgs> OnEventRemoved;

    void AddSubscription<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;

    void AddSubscription(Type eventType, Type eventHandlerType);

    void AddSubscription<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;

    void AddSubscription(Type eventType, Type eventHandlerType, string eventName);

    void RemoveSubscription<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;

    void RemoveSubscription(Type eventType, Type eventHandlerType);

    void RemoveSubscription<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>;

    void RemoveSubscription(Type eventType, Type eventHandlerType, string eventName);

    void Clear();
}
