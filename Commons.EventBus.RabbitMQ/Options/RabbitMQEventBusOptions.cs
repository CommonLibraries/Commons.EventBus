namespace Commons.EventBus.RabbitMQ
{
    public class RabbitMQEventBusOptions
    {
        public string ExchangeName { get; set; } = string.Empty;

        public string QueueName { get; set; } = string.Empty;

        public int MaximumPublishRetryCount { get; set; }
    }
}
