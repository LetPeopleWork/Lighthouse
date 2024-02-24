using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Implementation
{
    public interface IWorkItemCollectorService
    {
        Task UpdateFeaturesForProject(Project project);
    }
}