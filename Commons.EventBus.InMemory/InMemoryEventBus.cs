
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
            public IEvent Event { get; }

            public EventWrapper(string name, Type type, IEvent @event)
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

        private static async Task RunHandler<TEvent>(TEvent @event, IEventHandler<TEvent> handler, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await handler.HandleAsync(@event, cancellationToken);
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
                            var serviceProvider = scope.ServiceProvider;
                            var handlerTypeInterface = subscription.HandlerType.GetInterface(typeof(IEventHandler<>).Name);
                            if (handlerTypeInterface is null) continue;
                            var handler = serviceProvider.GetService(handlerTypeInterface);
                            if (handler is null) continue;

                            var runHandlerMethod = typeof(InMemoryEventBus).GetMethod(nameof(RunHandler), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                            if (runHandlerMethod is null) throw new MissingMethodException();

                            var genericRunHandlerMethod = runHandlerMethod?.MakeGenericMethod(subscription.EventType);


                            Func<Task> pipeline = () =>
                            {
                                var result = genericRunHandlerMethod?.Invoke(null, [item.Event, handler, cancellationToken]) as Task;
                                if (result is null) throw new InvalidOperationException("Could not invoke event handler.");
                                return result;
                            };

                            var middlewares = serviceProvider.GetServices<IEventMiddleware>();

                            foreach (var middleware in middlewares.Reverse())
                            {
                                var next = pipeline;
                                pipeline = () => middleware.Invoke(
                                        item.Event,
                                        new EventMiddlewareContext() { CancellationToken = cancellationToken },
                                        next
                                    );
                            }

                            try
                            {
                                await pipeline();
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
