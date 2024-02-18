using CMFTAspNet.Cache;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Implementation.AzureDevOps;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Factories
{
    public class WorkItemServiceFactory : IWorkItemServiceFactory
    {
        private readonly Cache<Type, IWorkItemService> cache = new Cache<Type, IWorkItemService>();

        public IWorkItemService CreateWorkItemServiceForTeam(ITeamConfiguration teamConfiguration)
        {
            var workItemSerivce = cache.Get(teamConfiguration.GetType());

            if (workItemSerivce == null)
            {

                switch (teamConfiguration)
                {
                    case AzureDevOpsTeamConfiguration:
                        workItemSerivce = new AzureDevOpsWorkItemService();
                        break;
                    default:
                        throw new NotSupportedException();
                }

                cache.Store(teamConfiguration.GetType(), workItemSerivce, TimeSpan.FromMinutes(2));
            }

            return workItemSerivce;
        }
    }
}
