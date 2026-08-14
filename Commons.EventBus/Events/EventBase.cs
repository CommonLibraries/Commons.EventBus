namespace Commons.EventBus.Events;

public abstract class EventBase : IEvent
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public Guid? CorrelationId { get; set; } = Guid.CreateVersion7();
}
