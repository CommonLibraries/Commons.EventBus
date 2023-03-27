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

        IDictionary<string, IList<Subscription>> GetSubscriptionsForEvent<TEvent>();

        IDictionary<string, IList<Subscription>> GetSubscriptionsForEvent(string eventName);

        event EventHandler<SubscriptionRemovedArgs> OnEventRemoved;

        void AddSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void RemoveSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void AddSubscription<TEventHandler>(string eventName)
            where TEventHandler : IEventHandler;

        void RemoveSubscription<TEventHandler>(string eventName)
            where TEventHandler : IEventHandler;

        void Clear();
    }
}
