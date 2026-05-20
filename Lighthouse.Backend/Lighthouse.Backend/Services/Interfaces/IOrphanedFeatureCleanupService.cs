namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IOrphanedFeatureCleanupService
    {
        Task<int> CleanupAsync(CancellationToken cancellationToken = default);
    }
}
