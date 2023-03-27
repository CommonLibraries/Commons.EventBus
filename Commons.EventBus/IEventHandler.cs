namespace Commons.EventBus
{
    public interface IEventHandler
    {
        Task HandleAsync(string @event, CancellationToken cancellationToken);
    }

    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    }
}
