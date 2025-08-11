using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.EventBus.SubscriptionManager
{
    public class Subscription
    {
        public Type EventType { get; protected set; }

        public Type HandlerType { get; protected set; }

        public Subscription(Type eventType, Type handlerType)
        {
            this.EventType = eventType;
            this.HandlerType = handlerType;
        }
    }
}
