using Lighthouse.Models;

namespace Lighthouse.Services.Implementation
{
    public interface IWorkItemCollectorService
    {
        Task UpdateFeaturesForProject(Project project);
    }
}