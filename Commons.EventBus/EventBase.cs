namespace Commons.EventBus
{
    public abstract class EventBase : IEvent
    {
        public Guid Id { get; } = Guid.CreateVersion7();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string CorrelationId { get; }

        public EventBase(string? correlationId)
        {
            this.Id = Guid.CreateVersion7();
            this.CreatedAt = DateTime.UtcNow;
            this.CorrelationId = correlationId ?? Guid.CreateVersion7().ToString();
        }
    }
}
