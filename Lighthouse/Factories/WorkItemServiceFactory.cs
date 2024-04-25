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
        private readonly IConfiguration configuration;

        public WorkItemServiceFactory(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IWorkItemService GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            var workItemSerivce = cache.Get(workTrackingSystem);

            if (workItemSerivce == null)
            {
                switch (workTrackingSystem)
                {
                    case WorkTrackingSystems.AzureDevOps:
                        workItemSerivce = new AzureDevOpsWorkItemService();
                        break;
                    case WorkTrackingSystems.Jira:
                        var lexoRankService = new LexoRankService();
                        workItemSerivce = new JiraWorkItemService(lexoRankService, new IssueFactory(configuration, lexoRankService));
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
