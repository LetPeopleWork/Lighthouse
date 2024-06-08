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
        private readonly IIssueFactory issueFactory;
        private readonly ILexoRankService lexoRankService;
        private readonly ILogger<WorkItemServiceFactory> workItemServiceFactoryLogger;
        private readonly ILogger<AzureDevOpsWorkItemService> azureDevOpsWorkItemServiceLogger;
        private readonly ILogger<JiraWorkItemService> jiraWorkItemServiceLogger;

        public WorkItemServiceFactory(
            IIssueFactory issueFactory, 
            ILexoRankService lexoRankService, 
            ILogger<WorkItemServiceFactory> workItemServiceFactoryLogger, 
            ILogger<AzureDevOpsWorkItemService> azureDevOpsWorkItemServiceLogger,
            ILogger<JiraWorkItemService> jiraWorkItemServiceLogger)
        {
            this.issueFactory = issueFactory;
            this.lexoRankService = lexoRankService;
            this.workItemServiceFactoryLogger = workItemServiceFactoryLogger;
            this.azureDevOpsWorkItemServiceLogger = azureDevOpsWorkItemServiceLogger;
            this.jiraWorkItemServiceLogger = jiraWorkItemServiceLogger;
        }


        public IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            workItemServiceFactoryLogger.LogDebug($"Getting Work Item Service for {workTrackingSystem}");
            var workItemSerivce = cache.Get(workTrackingSystem);

            if (workItemSerivce == null)
            {
                workItemServiceFactoryLogger.LogDebug($"Work Item Service for {workTrackingSystem} not found in the cache - creating");
                switch (workTrackingSystem)
                {
                    case WorkTrackingSystems.AzureDevOps:
                        workItemSerivce = new AzureDevOpsWorkItemService(azureDevOpsWorkItemServiceLogger);
                        break;
                    case WorkTrackingSystems.Jira:
                        workItemSerivce = new JiraWorkItemService(lexoRankService, issueFactory, jiraWorkItemServiceLogger);
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
