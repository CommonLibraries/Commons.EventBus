namespace Commons.EventBus;

public class EventMiddlewareContext
{
    public required string? Context { get; init; }
    public required CancellationToken CancellationToken { get; init; }
}
