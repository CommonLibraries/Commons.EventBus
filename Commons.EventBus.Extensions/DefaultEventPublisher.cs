using Commons.EventBus.Events;
using Commons.EventBus.Filters;

namespace Commons.EventBus.Extensions;

internal class DefaultEventPublisher : IEventPublisher
{
    private readonly IEventBus eventBus;
    private readonly IList<IEventFilter> eventFilters;
    public DefaultEventPublisher(IEventBus eventBus, IEnumerable<IEventFilter> eventFilters)
    {
        this.eventBus = eventBus;
        this.eventFilters = eventFilters.ToList();
    }

    private void OnEventPublishing(IEvent @event)
    {
        var eventPublishingContext = new EventPublishingContext();
        foreach (var filter in this.eventFilters)
        {
            filter.OnEventPublishing(@event, eventPublishingContext);
        }
    }

    private void OnEventPublished(IEvent @event)
    {
        var eventPublishedContext = new EventPublishedContext();
        foreach (var filter in this.eventFilters)
        {
            filter.OnEventPublished(@event, eventPublishedContext);
        }
    }

    private async Task OnEventPublishingAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var eventPublishingContext = new EventPublishingContext();
        foreach (var filter in this.eventFilters)
        {
            await filter.OnEventPublishingAsync(@event, eventPublishingContext, cancellationToken);
        }
    }

    private async Task OnEventPublishedAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        var eventPublishedContext = new EventPublishedContext();
        foreach (var filter in this.eventFilters)
        {
            await filter.OnEventPublishedAsync(@event, eventPublishedContext, cancellationToken);
        }
    }

    public void Publish(IEvent @event)
    {
        this.OnEventPublishing(@event);
        this.eventBus.Publish(@event);
        this.OnEventPublished(@event);
    }

    public void Publish(IEvent @event, string eventName)
    {
        this.OnEventPublishing(@event);
        this.eventBus.Publish(@event, eventName);
        this.OnEventPublished(@event);
    }

    public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
    {
        await this.OnEventPublishingAsync(@event, cancellationToken);
        await this.eventBus.PublishAsync(@event, cancellationToken);
        await this.OnEventPublishedAsync(@event, cancellationToken);
    }

    public async Task PublishAsync(IEvent @event, string eventName, CancellationToken cancellationToken = default)
    {
        await this.OnEventPublishingAsync(@event, cancellationToken);
        await this.eventBus.PublishAsync(@event, eventName, cancellationToken);
        await this.OnEventPublishedAsync(@event, cancellationToken);
    }
}
