using Microsoft.Extensions.DependencyInjection;
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
using System.Threading.Tasks;

namespace Commons.EventBus.RabbitMQ
{
    public class RabbitMQEventBus : IEventBus
    {
        private readonly IRabbitMQPersistentConnection persistentConnection;

        private readonly ISubscriptionMananager subscriptionMananager;

        private readonly RabbitMQEventBusOptions options;

        private IModel consumerChannel;

        private readonly IServiceScopeFactory serviceScopeFactory;

        private readonly ILogger logger;

        public RabbitMQEventBus(IRabbitMQPersistentConnection persistentConnection,
            ISubscriptionMananager subscriptionMananager,
            RabbitMQEventBusOptions options,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<RabbitMQEventBus> logger)
        {
            this.persistentConnection = persistentConnection;
            this.subscriptionMananager = subscriptionMananager;
            this.options = options;
            this.serviceScopeFactory = serviceScopeFactory;
            this.logger = logger;

            this.consumerChannel = this.CreateConsumerChannel();
            this.subscriptionMananager.OnEventRemoved += OnEventRemovedFromSubscriptionManager;
        }

        public Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
        {
            if (!this.persistentConnection.IsConnected)
            {
                this.persistentConnection.TryConnect();
            }

            var policy = RetryPolicy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(this.options.MaximumPublishRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, delay) =>
                {
                    this.logger.LogWarning(
                        ex,
                        "Could not publish event ${EventId}. Retry after {Timeout} seconds. Exception message: {ExceptionMessage}.",
                        @event.Id,
                        delay.TotalSeconds,
                        ex.Message);
                });

            var eventName = this.subscriptionMananager.GetEventName<TEvent>();
            
            using var channel = this.persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: this.options.ExchangeName, type: "direct");

            var message = JsonSerializer.Serialize<TEvent>(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;
                channel.BasicPublish(
                    exchange: this.options.ExchangeName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });

            return Task.CompletedTask;
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

            this.BindEventToQueue(eventName);
            this.subscriptionMananager.AddSubscription<TEvent, TEventHandler>(eventName);
            this.StartBasicConsumer();

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

        private IModel CreateConsumerChannel()
        {
            if (!this.persistentConnection.IsConnected)
            {
                this.persistentConnection.TryConnect();
            }

            this.logger.LogTrace("Creating a RabbitMQ consumer channel.");

            var channel = this.persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: this.options.ExchangeName, type: "direct");

            var queueDeclareOk = channel.QueueDeclare(queue: this.options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            // In case the queue name in settings object has null value, a generated name is provided by RabbitMQ broker.
            this.options.QueueName = queueDeclareOk.QueueName;

            channel.CallbackException += OnConsumerChannelCallbackException;

            this.logger.LogTrace("Created RabbitMQ consumer channel.");

            return channel;
        }

        private void OnConsumerChannelCallbackException(object? sender, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            this.logger.LogWarning(e.Exception, "Recreating RabbitMQ consumer channnel.");

            this.consumerChannel.Dispose();
            this.consumerChannel = CreateConsumerChannel();
            this.StartBasicConsumer();
        }

        private void StartBasicConsumer()
        {
            this.logger.LogTrace("Start RabbitMQ basic consumer.");

            if (this.consumerChannel is null)
            {
                this.logger.LogError($"{nameof(this.StartBasicConsumer)} can not call on {nameof(this.consumerChannel)} == null");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(this.consumerChannel);

            consumer.Received += OnConsumerReceived;

            this.consumerChannel.BasicConsume(
                queue: this.options.QueueName,
                autoAck: false,
                consumer: consumer);
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

            this.consumerChannel.BasicAck(args.DeliveryTag, multiple: false);
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
                    var task = concreteType.GetMethod(nameof(IEventHandler<Event>.HandleAsync))?.Invoke(handler, new object[] { @event }) as Task;
                    if (task is not null)
                    {
                        await task;
                    }
                }
            }

            this.logger.LogTrace("Processed event {EventName}.", eventName);
        }

        private void BindEventToQueue(string eventName)
        {
            var hasSubscriptions = this.subscriptionMananager.HasSubscriptionsForEvent(eventName);
            if (hasSubscriptions) return;

            if (!this.persistentConnection.IsConnected)
            {
                this.persistentConnection.TryConnect();
            }

            using var channel = this.persistentConnection.CreateModel();
            channel.QueueBind(
                queue: this.options.QueueName,
                exchange: this.options.ExchangeName,
                routingKey: eventName);
        }

        private void UnbindEventFromQueue(string eventName)
        {
            var hasSubscriptions = this.subscriptionMananager.HasSubscriptionsForEvent(eventName);
            if (hasSubscriptions) return;

            if (!this.persistentConnection.IsConnected)
            {
                this.persistentConnection.TryConnect();
            }

            using var channel = this.persistentConnection.CreateModel();
            channel.QueueUnbind(
                queue: this.options.QueueName,
                exchange: this.options.ExchangeName,
                routingKey: eventName);
        }

        private void OnEventRemovedFromSubscriptionManager(object? sender, SubscriptionRemovedArgs args)
        {
            this.UnbindEventFromQueue(args.EventName);
            this.logger.LogInformation("Unsubscribed from event {EventName}.", args.EventName);
        }
    }
}
