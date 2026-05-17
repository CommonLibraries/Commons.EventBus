namespace Commons.EventBus.RabbitMQ;

internal class EventWrapper
{
    public string Name { get; }
    public Type Type { get; }
    public object Event { get; }

    public EventWrapper(string name, Type type, object @event)
    {
        this.Name = name;
        this.Type = type;
        this.Event = @event;
    }
}
