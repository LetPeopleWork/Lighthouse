using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// What a bound source currently is, seen from one Portfolio: the remote verdict, plus the Features
    /// this Portfolio tracks among the work tagged against the source. The tagged count is kept
    /// alongside because it is the only thing that tells a source nobody tagged any work against apart
    /// from one whose work this Portfolio simply does not cover - two situations that are put right in
    /// two different places.
    /// </summary>
    public sealed record PortfolioSourcePreview(
        DeliverySourceResolution Resolution,
        IReadOnlyList<Feature> TrackedFeatures,
        int TaggedItemCount);

    /// <summary>
    /// Turns a set of bound source references into verdicts, and intersects the remote membership with
    /// the Features the Portfolio actually tracks.
    ///
    /// One resolver with two callers - create and the refresh re-sync - so the two can never drift on
    /// which verdicts are recoverable. Batched in one call per pass, because the cost of a refresh has
    /// to stay constant in the number of bound Deliveries.
    /// </summary>
    public interface IDeliverySourceResolver
    {
        Task<IReadOnlyDictionary<string, PortfolioSourcePreview>> ResolveForPortfolio(
            Portfolio portfolio, string sourceKey, IReadOnlyList<string> sourceReferences);

        /// <summary>
        /// Whether the connection behind this Portfolio says it offers the named source at all. A
        /// connection that cannot read remote delivery objects offers none, and so does a connection
        /// asked for a name it does not know - both are permanent answers about what exists here,
        /// which is why they must be told apart from a remote that could not be reached.
        /// </summary>
        bool OffersSource(Portfolio portfolio, string sourceKey);
    }
}
