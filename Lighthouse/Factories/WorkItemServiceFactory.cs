using Lighthouse.Cache;
using Lighthouse.Factories;
using Lighthouse.Services.Implementation.WorkItemServices;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;

namespace Lighthouse.Services.Factories
{
    public class WorkItemServiceFactory : IWorkItemServiceFactory
    {
        private readonly Cache<WorkTrackingSystems, IWorkItemService> cache = new Cache<WorkTrackingSystems, IWorkItemService>();
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<WorkItemServiceFactory> workItemServiceFactoryLogger;

        public WorkItemServiceFactory(
            IServiceProvider serviceProvider,
            ILogger<WorkItemServiceFactory> workItemServiceFactoryLogger)
        {
            this.serviceProvider = serviceProvider;
            this.workItemServiceFactoryLogger = workItemServiceFactoryLogger;
        }


        public IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            workItemServiceFactoryLogger.LogDebug("Getting Work Item Service for {workTrackingSystem}", workTrackingSystem);
            var workItemSerivce = cache.Get(workTrackingSystem);

            if (workItemSerivce == null)
            {
                workItemServiceFactoryLogger.LogDebug("Work Item Service for {workTrackingSystem} not found in the cache - creating", workTrackingSystem);
                switch (workTrackingSystem)
                {
                    case WorkTrackingSystems.AzureDevOps:
                        workItemSerivce = serviceProvider.GetRequiredService<AzureDevOpsWorkItemService>();
                        break;
                    case WorkTrackingSystems.Jira:
                        workItemSerivce = serviceProvider.GetRequiredService<JiraWorkItemService>();
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
