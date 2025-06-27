using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IFeatureHistoryService
    {
        Task ArchiveFeatures(IEnumerable<Feature> features);
        
        Task CleanupData();
    }
}