using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Commons.EventBus.InMemory.Extensions
{
    public static class InMemoryEventBusExtensions
    {
        public static IServiceCollection UseInMemoryEventBus(this IServiceCollection services)
        {
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            services.AddSingleton<IEventPublisher>(serviceProvider => serviceProvider.GetRequiredService<IEventBus>());
            services.AddSingleton<IEventSubscriber>(serviceProvider => serviceProvider.GetRequiredService<IEventBus>());
            services.AddTransient<ISubscriptionMananager, InMemorySubscriptionMananager>();
            return services;
        }

        public static IServiceCollection AddEventHandlers(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract) continue;

                var typeInterface = type.GetInterface(typeof(IEventHandler<>).Name);
                if (typeInterface is null) continue;

                services.AddTransient(typeInterface, type);
            }
            return services;
        }
    }
}
