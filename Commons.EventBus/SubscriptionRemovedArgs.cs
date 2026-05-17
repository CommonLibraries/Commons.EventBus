namespace Commons.EventBus;

public class SubscriptionRemovedArgs
{
    public string EventName { get; protected set; }

    public SubscriptionRemovedArgs(string eventName)
    {
        EventName = eventName;
    }
}
