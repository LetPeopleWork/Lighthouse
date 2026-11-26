using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Modules.Forecasting.Adapters.Background;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Application.WriteBack;
using Lighthouse.Backend.Modules.Forecasting.Domain;
using Lighthouse.Backend.Platform.Seeding;

namespace Lighthouse.Backend.Modules.Forecasting
{
    public static class ForecastingModule
    {
        public static IServiceCollection AddForecasting(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IForecastService, ForecastService>();
            serviceCollection.AddScoped<IForecastRealityCheckService, ForecastRealityCheckService>();
            serviceCollection.AddScoped<ForecastFilterRuleService>();
            serviceCollection.AddScoped<IForecastFilterRuleService>(sp => sp.GetRequiredService<ForecastFilterRuleService>());
            serviceCollection.AddScoped<ForecastingAnswersToFlowMetrics>();
            serviceCollection.AddScoped<IWriteBackService, WriteBackService>();
            serviceCollection.AddScoped<IWriteBackTriggerService, WriteBackTriggerService>();
            // One instance for the whole application: it is how the update queue tells the collector in a
            // scope which refresh round that scope is working for, and the queue itself lives that long.
            serviceCollection.AddSingleton<WriteBackRoundContext>();
            serviceCollection.AddScoped<IWriteBackCollector, WriteBackCollector>();
            serviceCollection.AddScoped<ISeeder, ForecastInputsForgottenAtStartSeeder>();
            serviceCollection.AddSingleton<IForecastUpdater, ForecastUpdater>();
            serviceCollection.AddSingleton<IDrawStreamFactory, DrawStreamFactory>();
            serviceCollection.AddSingleton(ForecastSimulationLimits.Default);
            serviceCollection.AddSingleton<ForecastSimulation>();
            // Built from the limits the container holds, so the lane has as many threads as a forecast may use runs at
            // once. The container builds it, so it also disposes it, after the hosted services have stopped.
            serviceCollection.AddSingleton(services => new ForecastLane(services.GetRequiredService<ForecastSimulationLimits>())
                .ReportingThreadsThatDidNotFinishTo(services.GetRequiredService<ILogger<ForecastLane>>()));

            return serviceCollection;
        }
    }
}
