using System.Reflection;

namespace Commons.EventBus.Extensions;

public interface IEventBusServiceBuilder
{
    IEventBusServiceBuilder UseEventBus<TEventBusImplementation>()
        where TEventBusImplementation : class, IEventBus;
    IEventBusServiceBuilder UseMiddleware<TMiddleware>()
        where TMiddleware : class, IEventMiddleware;
    IEventBusServiceBuilder AddEventHandlers(Assembly assembly);
    IEventBusServiceBuilder AddEventHandlers(Assembly assembly, string context);
}
