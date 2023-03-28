using System;

namespace Commons.EventBus
{
    public class SubscriptionRemovedArgs
    {
        public string EventName { get; protected set; }
        
        public SubscriptionRemovedArgs(string eventName)
        {
            EventName = eventName;
        }
    }

    public interface ISubscriptionMananager
    {
        bool IsEmpty { get; }

        string GetEventName<TEvent>()
            where TEvent : IEvent;

        Type GetEventType(string eventName);

        bool HasSubscriptionsForEvent<TEvent>()
            where TEvent : IEvent;

        bool HasSubscriptionsForEvent(string eventName);

        IEnumerable<Subscription> GetSubscriptionsForEvent<TEvent>()
            where TEvent : IEvent;

        IEnumerable<Subscription> GetSubscriptionsForEvent(string eventName);

        event EventHandler<SubscriptionRemovedArgs> OnEventRemoved;

        void AddSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void RemoveSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void AddSubscription<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void RemoveSubscription<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void Clear();
    }
}
