namespace Lighthouse.Backend.Services.Interfaces
{
    public enum FeatureMovePlacement
    {
        Placed,
        FeatureNotFound,
        TargetNotFound,
    }

    /// <summary>
    /// The only writer of a Feature's place (ADR-132). Every gesture in the UI — Top, Up, Down, Bottom, and
    /// "above/below a named Feature" — reduces to one call: put this Feature where that one stands.
    /// </summary>
    public interface IFeatureRankingService
    {
        /// <summary>
        /// Places <paramref name="featureId"/> against <paramref name="targetFeatureId"/>, before it or
        /// after it. A null target means the end of the order (D18's Move to Bottom).
        /// </summary>
        Task<FeatureMovePlacement> PlaceAsync(int featureId, int? targetFeatureId, bool placeBefore, CancellationToken cancellationToken = default);
    }
}
