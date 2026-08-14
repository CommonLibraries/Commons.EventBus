using Commons.EventBus.Events;

namespace Commons.EventBus.Middlewares;

public interface IEventMiddleware
{
    Task Invoke(IEvent @event, EventMiddlewareContext context, Func<Task> next);
}
