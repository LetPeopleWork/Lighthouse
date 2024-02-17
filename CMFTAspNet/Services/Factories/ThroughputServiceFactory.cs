using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Services.Implementation;

namespace CMFTAspNet.Services.Factories
{
    public class ThroughputServiceFactory
    {
        private readonly IWorkItemServiceFactory workItemServiceFactory;

        public ThroughputServiceFactory(IWorkItemServiceFactory workItemServiceFactory)
        {
            this.workItemServiceFactory = workItemServiceFactory;
        }

        public IThroughputService CreateThroughputServiceForTeam(Team team)
        {
            var workItemService = workItemServiceFactory.CreateWorkItemServiceForTeam(team.TeamConfiguration);
            return new ThroughputService(team, workItemService);
        }
    }
}
