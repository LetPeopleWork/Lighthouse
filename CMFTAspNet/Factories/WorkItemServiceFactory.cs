using CMFTAspNet.Cache;
using CMFTAspNet.Services.Implementation.AzureDevOps;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Services.Factories
{
    public class WorkItemServiceFactory : IWorkItemServiceFactory
    {
        private readonly Cache<WorkTrackingSystems, IWorkItemService> cache = new Cache<WorkTrackingSystems, IWorkItemService>();

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
                    default:
                        throw new NotSupportedException();
                }

                cache.Store(workTrackingSystem, workItemSerivce, TimeSpan.FromMinutes(2));
            }

            return workItemSerivce;
        }
    }
}
