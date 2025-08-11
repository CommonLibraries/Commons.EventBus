using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace Commons.EventBus.RabbitMQ
{
    public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory asyncConnectionFactory;

        private IConnection connection = null!;

        private readonly ILogger<DefaultRabbitMQPersistentConnection> logger;

        private readonly RabbitMQConnectionOptions options;

        private SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        public DefaultRabbitMQPersistentConnection(
            IOptions<RabbitMQConnectionOptions> options,
            ILogger<DefaultRabbitMQPersistentConnection> logger
            )
        {
            this.options = options.Value;
            this.logger = logger;

            this.asyncConnectionFactory = new ConnectionFactory()
            {
                HostName = this.options.HostName,
                Port = this.options.Port,
                UserName = this.options.UserName,
                Password = this.options.Password,
                AutomaticRecoveryEnabled = false
            };
        }

        public bool IsConnected => this.connection is { IsOpen: true } && !this.disposed;

        public async Task<IChannel> CreateChannel(CancellationToken cancellationToken = default)
        {
            if (!this.IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform thi action.");
            }

            return await this.connection.CreateChannelAsync();
        }

        public async Task<bool> TryConnect(CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("RabbitMQ Client is trying to connect.");

            this.locker.Wait();
            try
            {
                var policy = RetryPolicy
                        .Handle<SocketException>()
                        .Or<BrokerUnreachableException>()
                        .WaitAndRetryForeverAsync(retryAttempt => this.options.RetryDelay, (ex, delay) =>
                        {
                            this.logger.LogWarning(ex, "RabbitMQ Client could not connect because '{ExceptionMessage}'. Waiting for {TimeOut} seconds to try again.", ex.Message, delay.TotalSeconds);
                        });

                await policy.ExecuteAsync(async () =>
                {
                    this.connection = await this.asyncConnectionFactory.CreateConnectionAsync();
                });

                if (!this.IsConnected)
                {
                    this.logger.LogCritical("RabbitMQ Client could not connect.");
                    return false;
                }

                this.connection.ConnectionShutdownAsync += OnConnectionShutdown;
                this.connection.CallbackExceptionAsync += OnCallbackException;
                this.connection.ConnectionBlockedAsync += OnConnectionBlocked;
            }
            finally
            {
                this.locker.Release();
            }

            this.logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subcribed to failure events.", this.connection.Endpoint.HostName);
            return true;
        }

        private async Task OnConnectionBlocked(object? sender, global::RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
        {
            if (this.disposed) return;

            this.logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            await this.TryConnect();
        }

        private async Task OnCallbackException(object? sender, global::RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            if (this.disposed) return;

            this.logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            await this.TryConnect();
        }

        private async Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            if (this.disposed) return;

            this.logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            await this.TryConnect();
        }

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            try
            {
                if (disposing)
                {
                    this.connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                    this.connection.CallbackExceptionAsync -= OnCallbackException;
                    this.connection.ConnectionBlockedAsync -= OnConnectionBlocked;
                    this.connection.Dispose();
                }
            }
            catch (IOException ex)
            {
                this.logger.LogCritical(ex.ToString());
            }
            finally
            {
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RabbitMQPersistentConnection()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
