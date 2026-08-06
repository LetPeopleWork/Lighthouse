namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// ADR-135: the place a Feature holds across the whole instance, as a 1-based ordinal over every
    /// Feature in the order the forecast draws from — numbered before any result-set filter runs, so two
    /// rows shown next to each other may read 4 and 17.
    /// </summary>
    public interface IFeaturePositionMap
    {
        Task<IReadOnlyDictionary<int, int>> GetAsync(CancellationToken cancellationToken = default);
    }
}
