using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
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
            var workItemSerivce = cache.Get(workTrackingSystem);

            if (workItemSerivce == null)
            {
                logger.LogDebug("Work Item Service for {WorkTrackingSystem} not found in the cache - creating", workTrackingSystem);
                switch (workTrackingSystem)
                {
                    case WorkTrackingSystems.AzureDevOps:
                        workItemSerivce = serviceProvider.GetRequiredService<AzureDevOpsWorkTrackingConnector>();
                        break;
                    case WorkTrackingSystems.Jira:
                        workItemSerivce = serviceProvider.GetRequiredService<JiraWorkTrackingConnector>();
                        break;
                    case WorkTrackingSystems.Linear:
                        workItemSerivce = serviceProvider.GetRequiredService<LinearWorkTrackingConnector>();
                        break;
                    case WorkTrackingSystems.Csv:
                        workItemSerivce = serviceProvider.GetRequiredService<CsvWorkTrackingConnector>();
                        break;
                    default:
                        throw new NotSupportedException();
                }

                cache.Store(workTrackingSystem, workItemSerivce, TimeSpan.FromMinutes(2));
            }

            return workItemSerivce;
        }
    }
}
