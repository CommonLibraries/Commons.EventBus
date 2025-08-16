namespace Commons.EventBus
{
    public abstract class EventBase : IEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string CorrelationId { get; }

        public EventBase(string? correlationId)
        {
            this.Id = Guid.NewGuid();
            this.CreatedAt = DateTime.UtcNow;
            this.CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }
    }
}
