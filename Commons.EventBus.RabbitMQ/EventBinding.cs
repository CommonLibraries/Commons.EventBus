namespace Commons.EventBus.RabbitMQ;

internal class EventBinding
{
    public enum BindingType
    {
        Subscribe,
        Unsubscribe
    }

    public string EventName { get; }
    public BindingType Type { get; }

    public EventBinding(string eventName, BindingType type)
    {
        this.EventName = eventName;
        this.Type = type;
    }
}
