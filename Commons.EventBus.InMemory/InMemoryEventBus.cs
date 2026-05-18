using Commons.EventBus.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Channels;

namespace Commons.EventBus.InMemory;

public class InMemoryEventBus : IEventBus
{
    private readonly ISubscriptionMananager subscriptionMananager;
    private readonly IEventHandlerContextLookup eventHandlerContextLookup;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<InMemoryEventBus> logger;
    private readonly Channel<EventWrapper> eventChannel;
    private readonly IHostApplicationLifetime hostApplicationLifetime;

    private readonly Task loop;

    public InMemoryEventBus(
        ISubscriptionMananager subscriptionMananager,
        IEventHandlerContextLookup eventHandlerContextLookup,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<InMemoryEventBus> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        this.subscriptionMananager = subscriptionMananager;
        this.eventHandlerContextLookup = eventHandlerContextLookup;
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
        // No need to do anything here for in-memory event bus.
    }

    private void HandleSubscriptionRemoved(object? sender, SubscriptionRemovedArgs args)
    {
        // No need to do anything here for in-memory event bus.
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
                        var handlerType = handler.GetType();
                        var context = this.eventHandlerContextLookup.Get(handlerType);

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
                                new EventMiddlewareContext()
                                {
                                    Context = context,
                                    CancellationToken = cancellationToken
                                },
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

    public void Publish(IEvent @event)
    {
        var eventName = this.subscriptionMananager.GetEventName(@event.GetType());
        this.Publish(@event, eventName);
    }

    public void Publish(IEvent @event, string eventName)
    {
        var writer = this.eventChannel.Writer;
        var eventType = @event.GetType();
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

    public void Subscribe(Type eventType, Type eventHandler)
    {
        this.subscriptionMananager.AddSubscription(eventType, eventHandler);
    }

    public void Subscribe<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.subscriptionMananager.AddSubscription<TEvent, TEventHandler>(eventName);
    }

    public void Subscribe(Type eventType, Type eventHandler, string eventName)
    {
        this.subscriptionMananager.AddSubscription(eventType, eventHandler, eventName);
    }

    public void Unsubscribe<TEvent, TEventHandler>()
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.subscriptionMananager.RemoveSubscription<TEvent, TEventHandler>();
    }

    public void Unsubscribe(Type eventType, Type eventHandler)
    {
        this.subscriptionMananager.RemoveSubscription(eventType, eventHandler);
    }

    public void Unsubscribe<TEvent, TEventHandler>(string eventName)
        where TEvent : IEvent
        where TEventHandler : IEventHandler<TEvent>
    {
        this.subscriptionMananager.RemoveSubscription<TEvent, TEventHandler>(eventName);
    }

    public void Unsubscribe(Type eventType, Type eventHandler, string eventName)
    {
        this.subscriptionMananager.RemoveSubscription(eventType, eventHandler, eventName);
    }
}
