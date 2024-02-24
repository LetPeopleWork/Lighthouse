using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Implementation
{
    public interface IWorkItemCollectorService
    {
        Task<IEnumerable<Feature>> CollectFeaturesForProject(IEnumerable<Project> projects);
    }
}