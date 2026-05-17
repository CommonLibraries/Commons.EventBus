using System;
using Commons.EventBus.Contexts;

namespace Commons.EventBus.Extensions;

public class DefaultEventHandlerContextLookup : IEventHandlerContextLookup
{
    private readonly IDictionary<Type, string> contextLookup;
    public DefaultEventHandlerContextLookup(IDictionary<Type, string> contextLookup)
    {
        this.contextLookup = contextLookup;
    }

    public string? Get(Type eventHandlerType)
    {
        if (this.contextLookup.TryGetValue(eventHandlerType, out var context))
        {
            return context;
        }
        
        return null;
    }
}
