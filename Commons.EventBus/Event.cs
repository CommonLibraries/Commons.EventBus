namespace Commons.EventBus
{
    public abstract class Event : IEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
