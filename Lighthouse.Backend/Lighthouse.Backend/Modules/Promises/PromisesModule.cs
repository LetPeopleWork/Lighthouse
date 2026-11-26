using Lighthouse.Backend.Modules.Promises.Application;

namespace Lighthouse.Backend.Modules.Promises
{
    public static class PromisesModule
    {
        public static IServiceCollection AddPromises(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IDeliveryRuleService, DeliveryRuleService>();
            serviceCollection.AddScoped<IDeliverySourceResolver, DeliverySourceResolver>();
            serviceCollection.AddScoped<IDeliverySourceSyncService, DeliverySourceSyncService>();
            serviceCollection.AddScoped<IDeliveryForecastBlockRenderer, DeliveryForecastBlockRenderer>();
            serviceCollection.AddScoped<IDeliveryForecastPublishingService, DeliveryForecastPublishingService>();
            serviceCollection.AddScoped<DeliveryMetricValuesProjector>();

            return serviceCollection;
        }
    }
}
