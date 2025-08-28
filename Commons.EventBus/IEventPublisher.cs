namespace Commons.EventBus
{
    public interface IEventPublisher
    {
        void Publish<TEvent>(TEvent @event)
            where TEvent : IEvent;
    }
}
