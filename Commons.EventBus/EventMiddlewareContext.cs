namespace Commons.EventBus
{
    public class EventMiddlewareContext
    {
        public CancellationToken CancellationToken { get; init; } = default;
    }
}
