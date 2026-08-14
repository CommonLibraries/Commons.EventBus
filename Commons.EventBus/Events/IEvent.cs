namespace Commons.EventBus.Events;

public interface IEvent
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    Guid? CorrelationId { get; set; }
}
