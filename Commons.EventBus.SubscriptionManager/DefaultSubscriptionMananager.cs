using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.EventBus.SubscriptionManager
{
    public class DefaultSubscriptionMananager : ISubscriptionMananager
    {
        protected readonly IDictionary<string, Type> subscribedEvents;

        protected readonly IDictionary<string, IList<Subscription>> subscribedHandlers;

        public DefaultSubscriptionMananager()
        {
            this.subscribedEvents = new Dictionary<string, Type>();
            this.subscribedHandlers = new Dictionary<string, IList<Subscription>>();
        }

        public event EventHandler<SubscriptionAddedArgs>? OnEventAdded;
        public event EventHandler<SubscriptionRemovedArgs>? OnEventRemoved;

        protected void RaiseOnEventAdded(string eventName, Type eventType)
        {
            this.OnEventAdded?.Invoke(this, new SubscriptionAddedArgs(eventName));
        }

        protected void RaiseOnEventRemoved(string eventName)
        {
            this.OnEventRemoved?.Invoke(this, new SubscriptionRemovedArgs(eventName));
        }

        public bool IsEmpty
        {
            get => this.subscribedEvents.Count > 0;
        }

        public string GetEventName<TEvent>()
            where TEvent : IEvent
        {
            return typeof(TEvent).Name;
        }

        public Type GetEventType(string eventName)
        {
            if (!this.subscribedEvents.ContainsKey(eventName))
            {
                return null!;
            }

            return this.subscribedEvents[eventName];
        }

        public bool HasSubscriptionsForEvent<TEvent>()
           where TEvent : IEvent
        {
            string eventName = this.GetEventName<TEvent>();
            return this.HasSubscriptionsForEvent(eventName);
        }

        public bool HasSubscriptionsForEvent(string eventName)
        {
            if (!this.subscribedHandlers.ContainsKey(eventName))
            {
                return false;
            }

            bool hasSubscriptions = this.subscribedHandlers[eventName].Count > 0;
            return hasSubscriptions;
        }

        public IEnumerable<Subscription> GetSubscriptionsForEvent<TEvent>()
           where TEvent : IEvent
        {
            var eventName = this.GetEventName<TEvent>();
            return this.GetSubscriptionsForEvent(eventName);
        }

        public IEnumerable<Subscription> GetSubscriptionsForEvent(string eventName)
        {
            if (HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = this.subscribedHandlers[eventName];
                return subscriptions;
            }

            return new List<Subscription>();
        }

        public void AddSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            string eventName = this.GetEventName<TEvent>();
            this.AddSubscription<TEvent, TEventHandler>(eventName);
        }

        public void RemoveSubscription<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = this.GetEventName<TEvent>();
            this.RemoveSubscription<TEvent, TEventHandler>(eventName);
        }

        public void AddSubscription<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            if (!this.subscribedEvents.ContainsKey(eventName) || this.subscribedEvents[eventName] != typeof(TEvent))
            {
                this.subscribedEvents[eventName] = typeof(TEvent);
                this.subscribedHandlers[eventName] = new List<Subscription>();
            }

            var subscriptions = this.subscribedHandlers[eventName];
            bool alreadySubscribed = false;
            foreach (var subscription in subscriptions)
            {
                if (subscription.HandlerType == typeof(TEventHandler))
                {
                    alreadySubscribed = true;
                    break;
                }
            }

            if (!alreadySubscribed)
            {
                var subscription = new Subscription(typeof(TEvent), typeof(TEventHandler));
                subscriptions.Add(subscription);
            }
        }

        public void RemoveSubscription<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            if (!this.subscribedEvents.ContainsKey(eventName))
            {
                return;
            }

            var subscriptions = this.subscribedHandlers[eventName];
            Subscription removedSubscription = null!;
            foreach (var subscription in subscriptions)
            {
                if (subscription.HandlerType == typeof(TEventHandler))
                {
                    removedSubscription = subscription;
                    break;
                }
            }

            if (removedSubscription != null)
            {
                subscriptions.Remove(removedSubscription);

                if (subscriptions.Count == 0)
                {
                    this.subscribedHandlers.Remove(eventName);
                    this.subscribedEvents.Remove(eventName);
                }

                this.RaiseOnEventRemoved(eventName);
            }
        }

        public void Clear()
        {
            this.subscribedEvents.Clear();
            this.subscribedHandlers.Clear();
        }
    }
}
