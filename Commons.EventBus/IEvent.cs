namespace Commons.EventBus
{
    public interface IEvent
    {
        Guid Id { get; }
        DateTime CreatedAt { get; }
        string CorrelationId { get; }
    }
}
