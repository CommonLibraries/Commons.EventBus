using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.EventBus
{
    public class Subscription
    {
        public Type EventType { get; protected set; }

        public Type HandleType { get; protected set; }

        public Subscription(Type eventType, Type handleType)
        {
            this.EventType = eventType;
            this.HandleType = handleType;
        }
    }
}
