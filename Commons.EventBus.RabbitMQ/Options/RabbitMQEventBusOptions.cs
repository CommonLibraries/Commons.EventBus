namespace Commons.EventBus.RabbitMQ
{
    public class RabbitMQEventBusOptions
    {
        public string? ExchangeName { get; set; }

        public string? QueueName { get; set; }

        public int MaximumPublishRetryCount { get; set; }
    }
}
