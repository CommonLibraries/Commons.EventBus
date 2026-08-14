using Commons.EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Commons.EventBus.Extensions;

public static class Extensions
{
    public static IEventBusServiceBuilder AddEventBus(this IServiceCollection services)
    {
        return new DefaultEventBusServiceBuilder(services);
    }

    /// <summary>
    /// Add event handlers into subscription list of the event bus.
    /// These handlers must be registered first by using the IEventBusServiceBuilder.
    /// </summary>
    /// <param name="eventBus"></param>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static IEventBus UseEventHandlers(this IEventBus eventBus, Assembly assembly)
    {
        var types = assembly.GetExportedTypes();
        foreach (var type in types)
        {
            if (type.IsClass && !type.IsAbstract)
            {
                var typeInterface = type.GetInterface(typeof(IEventHandler<>).Name);
                if (typeInterface is null) continue;
                var eventType = typeInterface.GetGenericArguments()[0];
                var eventHandlerType = type;
                eventBus.Subscribe(eventType, eventHandlerType);
            }
        }
        return eventBus;
    }
}
