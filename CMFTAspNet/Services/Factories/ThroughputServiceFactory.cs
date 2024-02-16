using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.AzureDevOps;
using CMFTAspNet.Services.ThroughputService;

namespace CMFTAspNet.Services.Factories
{
    public class ThroughputServiceFactory
    {
        private readonly IAzureDevOpsWorkItemService azureDevOpsWorkItemService;

        public ThroughputServiceFactory(IAzureDevOpsWorkItemService azureDevOpsWorkItemService)
        {
            this.azureDevOpsWorkItemService = azureDevOpsWorkItemService;
        }

        public IThroughputService CreateThroughputServiceForTeam(Team team)
        {
            switch (team.TeamConfiguration)
            {
                case AzureDevOpsTeamConfiguration:
                    return new AzureDevOpsThroughputService(team, azureDevOpsWorkItemService);
                default:
                    throw new NotSupportedException();
            }            
        }
    }
}
