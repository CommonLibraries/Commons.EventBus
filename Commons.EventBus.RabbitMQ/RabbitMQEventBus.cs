using Commons.EventBus.SubscriptionManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Commons.EventBus.RabbitMQ
{
    public class RabbitMQEventBus : IEventBus
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

        private class EventBinding
        {
            public enum BindingType
            {
                Subscribe,
                Unsubscribe
            }

            public string EventName { get; }
            public BindingType Type { get; }

            public EventBinding(string eventName, BindingType type)
            {
                this.EventName = eventName;
                this.Type = type;
            }
        }

        private readonly IRabbitMQPersistentConnection persistentConnection;
        private readonly ISubscriptionMananager subscriptionMananager;
        private readonly RabbitMQEventBusOptions options;
        
        private IChannel? consumerChannel;
        
        private readonly ILogger logger;

        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IHostApplicationLifetime hostApplicationLifetime;
        
        private readonly Channel<EventWrapper> eventChannel;
        private readonly Channel<EventBinding> eventBindingChannel;

        private readonly Task loopTask;


        public RabbitMQEventBus(
            IRabbitMQPersistentConnection persistentConnection,
            ISubscriptionMananager subscriptionMananager,
            RabbitMQEventBusOptions options,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMQEventBus> logger,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            this.persistentConnection = persistentConnection;
            this.subscriptionMananager = subscriptionMananager;
            this.options = options;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;
            this.hostApplicationLifetime = hostApplicationLifetime;

            this.eventChannel = Channel.CreateUnbounded<EventWrapper>();
            this.eventBindingChannel = Channel.CreateUnbounded<EventBinding>();

            this.subscriptionMananager.OnEventRemoved += OnEventRemovedFromSubscriptionManager;

            this.hostApplicationLifetime.ApplicationStopping.Register(async () =>
            {
                this.eventChannel.Writer.Complete();
                await this.eventChannel.Reader.Completion;

                this.eventBindingChannel.Writer.Complete();
                await this.eventBindingChannel.Reader.Completion;

                if (this.consumerChannel is not null)
                {
                    await this.consumerChannel.DisposeAsync();
                }
            });

            var shutdownToken = this.hostApplicationLifetime.ApplicationStopping;
            this.loopTask = Task.Run(async () =>
            {
                await this.StartBasicConsumer(shutdownToken);

                _ = this.PublishEventsToQueues(shutdownToken);
                _ = this.HandleEventBindings(shutdownToken);
            });
        }

        private async Task PublishEventsToQueues(CancellationToken cancellationToken = default)
        {
            var reader = this.eventChannel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var item))
                {
                    if (!this.persistentConnection.IsConnected)
                    {
                        await this.persistentConnection.TryConnect();
                    }

                    using var channel = await this.persistentConnection.CreateChannel();
                    await channel.ExchangeDeclareAsync(exchange: this.options.ExchangeName, type: "direct");

                    var @event = item.Event as IEvent;
                    if (@event is null)
                    {
                        continue;
                    }

                    var message = JsonSerializer.Serialize(@event, item.Type);
                    var body = Encoding.UTF8.GetBytes(message);

                    var policy = RetryPolicy
                    .Handle<BrokerUnreachableException>()
                    .Or<SocketException>()
                    .WaitAndRetryAsync(this.options.MaximumPublishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, delay) =>
                    {
                        this.logger.LogWarning(
                            ex,
                            "Could not publish event ${EventId}. Retry after {Timeout} seconds. Exception message: {ExceptionMessage}.",
                            @event.Id,
                            delay.TotalSeconds,
                            ex.Message);
                    });

                    await policy.ExecuteAsync(async () =>
                    {
                        await channel.BasicPublishAsync(
                            exchange: this.options.ExchangeName,
                            routingKey: item.Name,
                            mandatory: true,
                            body: body);
                    });
                }
            }
        }

        public void Publish<TEvent>(TEvent @event) where TEvent : IEvent
        {
            var eventName = this.subscriptionMananager.GetEventName<TEvent>();
            var eventType = this.subscriptionMananager.GetEventType(eventName);
            if (!this.eventChannel.Writer.TryWrite(new EventWrapper(eventName, eventType, @event)))
            {
                throw new InvalidOperationException("Cannot publish event.");
            }
        }

        public void Subscribe<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = this.subscriptionMananager.GetEventName<TEvent>();
            this.Subscribe<TEvent, TEventHandler>(eventName);
        }

        public void Subscribe<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventHandlerName = typeof(TEventHandler).Name;

            this.logger.LogInformation("Subscribing to event {EventName} with {EventHandler}...", eventName, eventHandlerName);

            this.subscriptionMananager.AddSubscription<TEvent, TEventHandler>(eventName);

            this.logger.LogInformation("Subscribed to event {EventName} with {EventHandler}.", eventName, eventHandlerName);
        }

        public void Unsubscribe<TEvent, TEventHandler>()
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventName = this.subscriptionMananager.GetEventName<TEvent>();
            this.Unsubscribe<TEvent, TEventHandler>(eventName);
        }

        public void Unsubscribe<TEvent, TEventHandler>(string eventName)
            where TEvent : IEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            this.logger.LogInformation("Unsubscribing from event {EventName}.", eventName);
            this.subscriptionMananager.RemoveSubscription<TEvent, TEventHandler>(eventName);
        }

        private void OnEventRemovedFromSubscriptionManager(object? sender, SubscriptionRemovedArgs args)
        {
            if (!this.eventBindingChannel.Writer.TryWrite(new EventBinding(args.EventName, EventBinding.BindingType.Unsubscribe)))
            {
                throw new InvalidOperationException("Cannot unsubscribe from event.");
            }
            this.logger.LogInformation("Unsubscribed from event {EventName}.", args.EventName);
        }

        #region Consume events.
        private async Task StartBasicConsumer(CancellationToken cancellationToken = default)
        {
            if (this.consumerChannel is null)
            {
                this.consumerChannel = await this.CreateConsumerChannel();
            }

            this.logger.LogTrace("Start RabbitMQ basic consumer.");

            if (this.consumerChannel is null)
            {
                this.logger.LogError($"{nameof(this.StartBasicConsumer)} can not call on {nameof(this.consumerChannel)} == null");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(this.consumerChannel);

            consumer.ReceivedAsync += OnConsumerReceived;

            await this.consumerChannel.BasicConsumeAsync(
                queue: this.options.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        private async Task<IChannel> CreateConsumerChannel(CancellationToken cancellationToken = default)
        {
            if (!this.persistentConnection.IsConnected)
            {
                await this.persistentConnection.TryConnect();
            }

            this.logger.LogTrace("Creating a RabbitMQ consumer channel.");

            var channel = await this.persistentConnection.CreateChannel();

            await channel.ExchangeDeclareAsync(exchange: this.options.ExchangeName, type: "direct", cancellationToken: cancellationToken);

            var queueDeclareOk = await channel.QueueDeclareAsync(queue: this.options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // In case the queue name in settings object has null value, a generated name is provided by RabbitMQ broker.
            this.options.QueueName = queueDeclareOk.QueueName;

            channel.CallbackExceptionAsync += OnConsumerChannelCallbackException;

            this.logger.LogTrace("Created RabbitMQ consumer channel.");

            return channel;
        }

        private async Task OnConsumerChannelCallbackException(object? sender, CallbackExceptionEventArgs args)
        {
            this.logger.LogWarning(args.Exception, "Recreating RabbitMQ consumer channnel.");

            this.consumerChannel?.Dispose();
            this.consumerChannel = await this.CreateConsumerChannel(args.CancellationToken);
            await this.StartBasicConsumer();
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs args)
        {
            var eventName = args.RoutingKey;
            var message = Encoding.UTF8.GetString(args.Body.Span);

            try
            {
                await this.ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error while processign message: \"{Message}\".", message);
            }

            this.consumerChannel?.BasicAckAsync(args.DeliveryTag, multiple: false);
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            this.logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

            if (!this.subscriptionMananager.HasSubscriptionsForEvent(eventName))
            {
                this.logger.LogWarning("No subscription for RabbitMQ event: {EventName}.", eventName);
                return;
            }

            await using var scope = this.serviceScopeFactory.CreateAsyncScope();

            var subscriptions = this.subscriptionMananager.GetSubscriptionsForEvent(eventName);
            foreach (var subscription in subscriptions)
            {
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler is null)
                {
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message, subscription.EventType);
                if (@event is not null)
                {
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(subscription.EventType);
                    var task = concreteType.GetMethod(nameof(IEventHandler<EventBase>.HandleAsync))?.Invoke(handler, new object[] { @event }) as Task;
                    if (task is not null)
                    {
                        await task;
                    }
                }
            }

            this.logger.LogTrace("Processed event {EventName}.", eventName);
        }
        #endregion

        #region Bind / Unbind events.
        private async Task HandleEventBindings(CancellationToken cancellationToken = default)
        {
            var reader = this.eventBindingChannel.Reader;
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var item))
                {
                    if (!this.persistentConnection.IsConnected)
                    {
                        await this.persistentConnection.TryConnect();
                    }

                    using var channel = this.persistentConnection.CreateChannel();
                    if (item.Type == EventBinding.BindingType.Subscribe)
                    {
                        _ = this.BindEventToQueue(item.EventName);
                    }
                    else if (item.Type == EventBinding.BindingType.Unsubscribe)
                    {
                        _ = this.UnbindEventFromQueue(item.EventName);
                    }
                    else
                    {

                    }
                }
            }
        }

        private async Task BindEventToQueue(string eventName)
        {
            var hasSubscriptions = this.subscriptionMananager.HasSubscriptionsForEvent(eventName);
            if (hasSubscriptions) return;

            if (!this.persistentConnection.IsConnected)
            {
                await this.persistentConnection.TryConnect();
            }

            using var channel = await this.persistentConnection.CreateChannel();
            await channel.QueueBindAsync(
                queue: this.options.QueueName,
                exchange: this.options.ExchangeName,
                routingKey: eventName);
        }

        private async Task UnbindEventFromQueue(string eventName)
        {
            var hasSubscriptions = this.subscriptionMananager.HasSubscriptionsForEvent(eventName);
            if (hasSubscriptions) return;

            if (!this.persistentConnection.IsConnected)
            {
                await this.persistentConnection.TryConnect();
            }

            using var channel = await this.persistentConnection.CreateChannel();
            await channel.QueueUnbindAsync(
                queue: this.options.QueueName,
                exchange: this.options.ExchangeName,
                routingKey: eventName);
        }
        #endregion
    }
}
