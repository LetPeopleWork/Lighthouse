using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Persistence;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;

namespace Lighthouse.Backend.Modules.FlowMetrics
{
    public static class FlowMetricsModule
    {
        public static IServiceCollection AddFlowMetrics(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IBlockedCountSnapshotRepository, BlockedCountSnapshotRepository>();
            serviceCollection.AddScoped<IPercentilesOverTimeSnapshotRepository, PercentilesOverTimeSnapshotRepository>();
            serviceCollection.AddScoped<IPercentileSnapshotWriter, PercentileSnapshotWriter>();
            serviceCollection.AddScoped<IProcessBehaviorSnapshotRepository, ProcessBehaviorSnapshotRepository>();
            serviceCollection.AddScoped<IProcessBehaviorSnapshotWriter, ProcessBehaviorSnapshotWriter>();
            serviceCollection.AddSingleton(ICycleTimeOutlierDefinition.Default);
            serviceCollection.AddScoped<ITeamMetricsService, TeamMetricsService>();
            serviceCollection.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
            // The shipped read is registered as itself and wrapped: the interface resolves to the
            // wrapper, so opening a chart both answers from what is stored and mentions which days of
            // the window were missing. Both controllers keep asking for the interface and are unaware.
            serviceCollection.AddScoped<PercentilesOverTimeSeriesQuery>();
            serviceCollection.AddScoped<IOverTimeGapReconciler, OverTimeGapReconciler>();
            serviceCollection.AddScoped<IPercentilesOverTimeSeriesQuery, GapAskingPercentilesOverTimeSeriesQuery>();
            // Registered as itself as well as as the background job, because emptying the queue is
            // something a caller can ask for at a moment of its choosing - which is the only way a
            // test can observe this at all, since a test host runs no background work.
            serviceCollection.AddSingleton<OverTimeHistoryFiller>();
            serviceCollection.AddSingleton<IOverTimeHistoryFiller>(services => services.GetRequiredService<OverTimeHistoryFiller>());
            serviceCollection.AddSingleton<IOverTimeHistoryFillActivity>(services => services.GetRequiredService<OverTimeHistoryFiller>());
            serviceCollection.AddHostedService(services => services.GetRequiredService<OverTimeHistoryFiller>());
            serviceCollection.AddScoped<ProcessBehaviorSeriesQuery>();
            serviceCollection.AddScoped<IProcessBehaviorSeriesQuery, GapAskingProcessBehaviorSeriesQuery>();

            return serviceCollection;
        }
    }
}
