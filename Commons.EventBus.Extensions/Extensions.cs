using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Commons.EventBus.Extensions;

public static class Extensions
{
    public static IEventBusServiceBuilder ConfigureEventBus(this IServiceCollection services)
    {
        return new DefaultEventBusServiceBuilder(services);
    }

    public static IEventBus UseEventHandlers(this IEventBus eventBus, Assembly assembly)
    {
        var types = assembly.GetExportedTypes();
        foreach (var type in types)
        {
            if (type.IsClass && !type.IsAbstract)
            {
                var typeInterface = type.GetInterface(nameof(IEventHandler<>));
                if (typeInterface is null) continue;

                var eventType = typeInterface.GetGenericArguments()[0];
                eventBus.Subscribe(typeInterface, eventType);
            }
        }
        return eventBus;
    }
}
