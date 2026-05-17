using System;

namespace Commons.EventBus.Contexts;

public interface IEventHandlerContextLookup
{
    string? Get(Type eventHandlerType);
}
