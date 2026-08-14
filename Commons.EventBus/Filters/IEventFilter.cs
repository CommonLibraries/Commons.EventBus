using Commons.EventBus.Events;

namespace Commons.EventBus.Filters;

public interface IEventFilter
{
    void OnEventPublishing(IEvent @event, EventPublishingContext context);
    void OnEventPublished(IEvent @event, EventPublishedContext context);
    Task OnEventPublishingAsync(IEvent @event, EventPublishingContext context, CancellationToken cancellationToken = default);
    Task OnEventPublishedAsync(IEvent @event, EventPublishedContext context, CancellationToken cancellationToken = default);
}
