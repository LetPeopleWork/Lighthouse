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

        public WorkItemServiceFactory(IIssueFactory issueFactory, ILexoRankService lexoRankService)
        {
            this.issueFactory = issueFactory;
            this.lexoRankService = lexoRankService;
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
                        workItemSerivce = new JiraWorkItemService(lexoRankService, issueFactory);
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
