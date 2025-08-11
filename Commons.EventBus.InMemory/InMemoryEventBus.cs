
using Commons.EventBus.SubscriptionManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;

namespace Commons.EventBus.InMemory
{
    public class InMemoryEventBus : IEventBus
    {
        private class EventWrapper
        {
            public string Name { get; }
            public Type Type { get; }
            public object Event { get; }

            public EventWrapper(string name, Type type, object @event)
            {
                this.Name = name;
                this.Type = type;
                this.Event = @event;
            }
        }

        private readonly ISubscriptionMananager subscriptionMananager;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly ILogger<InMemoryEventBus> logger;
        private readonly Channel<EventWrapper> eventChannel;
        private readonly IHostApplicationLifetime hostApplicationLifetime;

        private readonly Task loop;

        public InMemoryEventBus(
            ISubscriptionMananager subscriptionMananager,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<InMemoryEventBus> logger,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            this.subscriptionMananager = subscriptionMananager;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
            this.hostApplicationLifetime = hostApplicationLifetime;

            this.eventChannel = Channel.CreateUnbounded<EventWrapper>();

            this.subscriptionMananager.OnEventAdded += this.HandleSubscriptionAdded;
            this.subscriptionMananager.OnEventRemoved += this.HandleSubscriptionRemoved;

            this.hostApplicationLifetime.ApplicationStopping.Register(async () =>
            {
                await this.eventChannel.Reader.Completion;
            });

            var stoppingToken = this.hostApplicationLifetime.ApplicationStopping;
            this.loop = Task.Run(async () =>
            {
                await this.HandleEvents(stoppingToken);
            });
        }

        private void HandleSubscriptionAdded(object? sender, SubscriptionAddedArgs args)
        {

        }

        private void HandleSubscriptionRemoved(object? sender, SubscriptionRemovedArgs args)
        {

        }

        private async Task HandleEvents(CancellationToken cancellationToken = default)
        {
            var reader = this.eventChannel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var item))
                {
                    var subscriptions = this.subscriptionMananager.GetSubscriptionsForEvent(item.Name);
                    foreach (var subscription in subscriptions)
                    {
                        await using (var scope = this.serviceScopeFactory.CreateAsyncScope())
                        {
                            var handler = ActivatorUtilities.CreateInstance(scope.ServiceProvider, subscription.HandlerType, item.Event, cancellationToken);
                            if (handler is null)
                            {
                                continue;
                            }

                            var concreteType = typeof(IEventHandler<>).MakeGenericType(subscription.EventType, typeof(CancellationToken));
                            var handleMethod = concreteType.GetMethod("HandleAsync");
                            if (handleMethod is not null)
                            {
                                var result = handleMethod.Invoke(handler, [item.Event, cancellationToken]) as Task;
                                if (result is not null)
                                {
                                    try
                                    {
                                        await result;
                                        this.logger.LogTrace("Processed event {EventName}.", item.Name);
                                    }
                                    catch (Exception ex)
                                    {
                                        this.logger.LogError(ex, "Could not process event.");
                                    }
                                    finally
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            var writer = this.eventChannel.Writer;

            var eventName = this.subscriptionMananager.GetEventName<TEvent>();
            var eventType = this.subscriptionMananager.GetEventType(eventName);
            if (!writer.TryWrite(new EventWrapper(eventName, eventType, @event)))
            {
                throw new InvalidOperationException("Could not publish event.");
            }
        }

        public void Subscribe<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            this.subscriptionMananager.AddSubscription<TEvent, TEventHandler>();
        }

        public void Subscribe<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            this.subscriptionMananager.AddSubscription<TEvent, TEventHandler>(eventName);
        }

        public void Unsubscribe<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            this.subscriptionMananager.RemoveSubscription<TEvent, TEventHandler>();
        }

        public void Unsubscribe<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            this.subscriptionMananager.RemoveSubscription<TEvent, TEventHandler>(eventName);
        }
    }
}
