namespace Commons.EventBus;

public class SubscriptionAddedArgs
{
    public string EventName { get; protected set; }

    public SubscriptionAddedArgs(string eventName)
    {
        this.EventName = eventName;
    }
}
