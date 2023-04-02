using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons.EventBus.Subscriptions.InMemory.Extensions
{
    public static class InMemorySubscriptionManangerServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemorySubscriptionManager(this IServiceCollection services)
        {
            services.AddTransient<ISubscriptionMananager, InMemorySubscriptionMananager>();
            return services;
        }
    }
}
