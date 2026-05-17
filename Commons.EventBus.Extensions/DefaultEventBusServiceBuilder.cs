using System.Reflection;
using Commons.EventBus.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace Commons.EventBus.Extensions;

internal class DefaultEventBusServiceBuilder : IEventBusServiceBuilder
{
    private readonly IDictionary<Type, string> contextLookup;
    private readonly IServiceCollection services;
    public DefaultEventBusServiceBuilder(IServiceCollection services)
    {
        this.services = services;
        this.contextLookup = new Dictionary<Type, string>();
    }

    public IEventBusServiceBuilder UseEventBus<TEventBusImplementation>()
        where TEventBusImplementation : class, IEventBus
    {
        this.services.AddTransient<ISubscriptionMananager, InMemorySubscriptionMananager>();
        this.services.AddTransient<IEventHandlerContextLookup>(serviceProvider => new DefaultEventHandlerContextLookup(this.contextLookup));
        this.services.AddSingleton(typeof(IEventBus), typeof(TEventBusImplementation));
        this.services.AddSingleton(typeof(IEventPublisher), serviceProvider => serviceProvider.GetRequiredService<IEventBus>());
        this.services.AddSingleton(typeof(IEventSubscriber), serviceProvider => serviceProvider.GetRequiredService<IEventBus>());
        return this;
    }

    public IEventBusServiceBuilder UseMiddleware<TMiddleware>()
        where TMiddleware : class, IEventMiddleware
    {
        this.services.AddTransient<IEventMiddleware, TMiddleware>();
        return this;
    }

    public IEventBusServiceBuilder AddEventHandlers(Assembly assembly)
    {
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var typeInterface = type.GetInterface(typeof(IEventHandler<>).Name);
            if (typeInterface is null) continue;

            this.services.AddTransient(typeInterface, type);
        }
        return this;
    }

    public IEventBusServiceBuilder AddEventHandlers(Assembly assembly, string context)
    {
        var types = assembly.GetTypes();
        foreach (var type in types)
        {
            if (!type.IsClass || type.IsAbstract) continue;

            var typeInterface = type.GetInterface(typeof(IEventHandler<>).Name);
            if (typeInterface is null) continue;

            this.services.AddTransient(typeInterface, type);
            this.contextLookup[type] = context;
        }
        return this;
    }
}
