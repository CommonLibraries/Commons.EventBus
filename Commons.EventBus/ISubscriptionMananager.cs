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

        bool HasSubscriptionsForEvent<TEvent>();

        bool HasSubscriptionsForEvent(string eventName);

        string GetEventName<TEvent>()
            where TEvent : IEvent;

        Type GetEventType(string eventName);

        IDictionary<string, IList<Subscription>> GetSubscriptionsForEvent<TEvent>()
            where TEvent : IEvent;

        IDictionary<string, IList<Subscription>> GetSubscriptionsForEvent(string eventName);

        event EventHandler<SubscriptionRemovedArgs> OnEventRemoved;

        void AddSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void RemoveSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void AddSubscription<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler;

        void RemoveSubscription<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler;

        void Clear();
    }
}
