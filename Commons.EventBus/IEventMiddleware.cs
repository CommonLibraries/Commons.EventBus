namespace Commons.EventBus
{
    public interface IEventMiddleware
    {
        Task Invoke(IEvent @event, EventMiddlewareContext context, Func<Task> next);
    }
}
