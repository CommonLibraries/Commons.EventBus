using RabbitMQ.Client;

namespace Commons.EventBus.RabbitMQ
{
    public interface IRabbitMQPersistentConnection : IDisposable
    {
        bool IsConnected { get; }

        Task<bool> TryConnect(CancellationToken cancellationToken = default);

        Task<IChannel> CreateChannel(CancellationToken cancellationToken = default);
    }
}
