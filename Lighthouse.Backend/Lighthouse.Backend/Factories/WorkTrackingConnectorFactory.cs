using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Factories
{
    public class WorkTrackingConnectorFactory : IWorkTrackingConnectorFactory
    {
        private readonly Cache<WorkTrackingSystems, IWorkTrackingConnector> cache = new Cache<WorkTrackingSystems, IWorkTrackingConnector>();
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<WorkTrackingConnectorFactory> logger;

        public WorkTrackingConnectorFactory(
            IServiceProvider serviceProvider,
            ILogger<WorkTrackingConnectorFactory> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public IWorkTrackingConnector GetWorkTrackingConnector(WorkTrackingSystems workTrackingSystem)
        {
            logger.LogDebug("Getting Work Item Service for {WorkTrackingSystem}", workTrackingSystem);
            var workItemSerivce = cache.Get(workTrackingSystem);

            if (workItemSerivce == null)
            {
                logger.LogDebug("Work Item Service for {WorkTrackingSystem} not found in the cache - creating", workTrackingSystem);                switch (workTrackingSystem)
                {
                    case WorkTrackingSystems.AzureDevOps:
                        workItemSerivce = serviceProvider.GetRequiredService<AzureDevOpsWorkTrackingConnector>();
                        break;
                    case WorkTrackingSystems.Jira:
                        workItemSerivce = serviceProvider.GetRequiredService<JiraWorkTrackingConnector>();
                        break;
                    case WorkTrackingSystems.JiraOAuth:
                        workItemSerivce = serviceProvider.GetRequiredService<JiraOAuthWorkTrackingConnector>();
                        break;
                    case WorkTrackingSystems.Linear:
                        workItemSerivce = serviceProvider.GetRequiredService<LinearWorkTrackingConnector>();
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
