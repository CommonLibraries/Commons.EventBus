namespace Commons.EventBus.InMemory;

/// <summary>
/// Data structure to wrap events in the memory event bus.
/// </summary>
internal class EventWrapper
{
    public string Name { get; }
    public Type Type { get; }
    public IEvent Event { get; }

    public EventWrapper(string name, Type type, IEvent @event)
    {
        this.Name = name;
        this.Type = type;
        this.Event = @event;
    }
}
