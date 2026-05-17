
using Commons.EventBus.Extensions;

namespace Commons.EventBus.InMemory;

public static class Extensions
{
    public static IEventBusServiceBuilder UseInMemoryEventBus(this IEventBusServiceBuilder builder)
    {
        return builder.UseEventBus<InMemoryEventBus>();
    }
}
