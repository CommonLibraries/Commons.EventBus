using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.EventBus.RabbitMQ.Extensions
{
    public static class RabbitMQEventBusServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMQEventBus(
            this IServiceCollection services,
            IConfiguration namedConfigurationSection)
        {
            services.Configure<RabbitMQEventBusOptions>(namedConfigurationSection);
            services.Configure<RabbitMQEventBusOptions>(namedConfigurationSection);
            
            services.AddTransient<IRabbitMQPersistentConnection, DefaultRabbitMQPersistentConnection>();
            services.AddSingleton<IEventBus, RabbitMQEventBus>();
            
            return services;
        }
    }
}
