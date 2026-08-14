using Commons.EventBus.Events;

namespace Commons.EventBus.Filters;

public abstract class EventFilter : IEventFilter
{
    public virtual void OnEventPublishing(IEvent @event, EventPublishingContext context)
    {
        return;
    }

    public virtual void OnEventPublished(IEvent @event, EventPublishedContext context)
    {
        return;
    }

    public virtual Task OnEventPublishingAsync(IEvent @event, EventPublishingContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    public virtual Task OnEventPublishedAsync(IEvent @event, EventPublishedContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
