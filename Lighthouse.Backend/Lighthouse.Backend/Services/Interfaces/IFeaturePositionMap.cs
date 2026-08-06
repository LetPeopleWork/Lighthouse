namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// The 1-based place each Feature holds across the whole instance, numbered before any result-set
    /// filter runs (ADR-135).
    /// </summary>
    public interface IFeaturePositionMap
    {
        Task<IReadOnlyDictionary<int, int>> GetAsync(CancellationToken cancellationToken = default);
    }
}
