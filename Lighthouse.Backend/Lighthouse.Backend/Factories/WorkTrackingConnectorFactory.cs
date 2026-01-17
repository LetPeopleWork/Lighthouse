using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Factories
{
    public class WorkTrackingConnectorFactory(
        IServiceProvider serviceProvider,
        ILogger<WorkTrackingConnectorFactory> logger)
        : IWorkTrackingConnectorFactory
    {
        private readonly Cache<WorkTrackingSystems, IWorkTrackingConnector> cache = new Cache<WorkTrackingSystems, IWorkTrackingConnector>();

        public IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem)
        {
            logger.LogDebug("Getting Work Item Service for {WorkTrackingSystem}", workTrackingSystem);
            var workItemService = cache.Get(workTrackingSystem);

            if (workItemService != null)
            {
                return workItemService;
            }

            logger.LogDebug("Work Item Service for {WorkTrackingSystem} not found in the cache - creating", workTrackingSystem);
            workItemService = workTrackingSystem switch
            {
                WorkTrackingSystems.AzureDevOps => serviceProvider
                    .GetRequiredService<IAzureDevOpsWorkTrackingConnector>(),
                WorkTrackingSystems.Jira => serviceProvider.GetRequiredService<IJiraWorkTrackingConnector>(),
                WorkTrackingSystems.Linear => serviceProvider.GetRequiredService<LinearWorkTrackingConnector>(),
                WorkTrackingSystems.Csv => serviceProvider.GetRequiredService<CsvWorkTrackingConnector>(),
                _ => throw new NotSupportedException()
            };

            cache.Store(workTrackingSystem, workItemService, TimeSpan.FromMinutes(2));

            return workItemService;
        }
    }
}
