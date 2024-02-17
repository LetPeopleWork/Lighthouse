using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Implementation.AzureDevOps;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Factories
{
    public class WorkItemServiceFactory : IWorkItemServiceFactory
    {
        public IWorkItemService CreateWorkItemServiceForTeam(ITeamConfiguration teamConfiguration)
        {
            switch (teamConfiguration)
            {
                case AzureDevOpsTeamConfiguration:
                    return new AzureDevOpsWorkItemService();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
