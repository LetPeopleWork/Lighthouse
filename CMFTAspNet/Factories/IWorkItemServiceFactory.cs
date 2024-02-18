using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Factories
{
    public interface IWorkItemServiceFactory
    {
        IWorkItemService CreateWorkItemServiceForTeam(ITeamConfiguration teamConfiguration);
    }
}