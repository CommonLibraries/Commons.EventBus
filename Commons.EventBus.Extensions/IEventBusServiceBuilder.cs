using Commons.EventBus.Filters;
using Commons.EventBus.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Commons.EventBus.Extensions;

public interface IEventBusServiceBuilder
{
    IServiceCollection Services { get; }
    IEventBusServiceBuilder UseEventBus<TEventBusImplementation>()
        where TEventBusImplementation : class, IEventBus;
    IEventBusServiceBuilder UseFilter<TFilter>()
        where TFilter : class, IEventFilter;
    IEventBusServiceBuilder UseMiddleware<TMiddleware>()
        where TMiddleware : class, IEventMiddleware;
    IEventBusServiceBuilder AddEventHandlers(Assembly assembly);
    IEventBusServiceBuilder AddEventHandlers(Assembly assembly, string context);
}
