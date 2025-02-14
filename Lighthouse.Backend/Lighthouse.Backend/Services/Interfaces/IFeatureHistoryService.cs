using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IFeatureHistoryService
    {
        Task ArchiveFeature(Feature feature);
        
        Task CleanupData();
    }
}