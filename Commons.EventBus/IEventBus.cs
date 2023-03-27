namespace Commons.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event)
            where TEvent : IEvent;

        void Subscribe<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void Unsubscribe<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>;

        void Subscribe(string eventName, Type eventHandler);
        
        void Unsubscribe(string eventName, Type eventHandler);
    }
}
