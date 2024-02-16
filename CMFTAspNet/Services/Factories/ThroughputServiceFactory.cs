using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.ThroughputService;

namespace CMFTAspNet.Services.Factories
{
    public class ThroughputServiceFactory
    {
        public IThroughputService CreateThroughputServiceForTeam(Team team)
        {
            switch (team.TeamConfiguration)
            {
                case AzureDevOpsTeamConfiguration:
                    return new AzureDevOpsThroughputService();
            }


            throw new NotSupportedException();
        }
    }
}
