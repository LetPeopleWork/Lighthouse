using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation
{
    public interface IWorkItemCollectorService
    {
        Task UpdateFeaturesForProject(Project project);
    }
}