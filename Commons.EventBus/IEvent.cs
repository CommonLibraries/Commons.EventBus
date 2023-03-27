namespace Commons.EventBus
{
    public interface IEvent
    {
        Guid Id { get; set; }
        DateTime CreatedAt { get; set; }
    }
}
