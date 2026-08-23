using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

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
        Models.DeliverySources.DeliverySourceResolution Resolution,
        IReadOnlyList<Models.Feature> TrackedFeatures,
        int TaggedItemCount);
}

namespace Lighthouse.Backend.Services.Implementation.DeliverySources
{
    public class DeliverySourceResolver(IWorkTrackingConnectorFactory workTrackingConnectorFactory) : IDeliverySourceResolver
    {
        public async Task<IReadOnlyDictionary<string, PortfolioSourcePreview>> ResolveForPortfolio(
            Portfolio portfolio, string sourceKey, IReadOnlyList<string> sourceReferences)
        {
            ArgumentNullException.ThrowIfNull(portfolio);
            ArgumentNullException.ThrowIfNull(sourceReferences);

            var connection = portfolio.WorkTrackingSystemConnection;
            var connector = workTrackingConnectorFactory.GetWorkTrackingConnector(connection.WorkTrackingSystem);

            if (connector is not IDeliverySourceProvider provider)
            {
                return NothingCouldBeAsked(sourceReferences, DeliverySourceUnavailableReason.CapabilityWithdrawn);
            }

            var resolutions = await provider.ResolveMany(connection, sourceKey, sourceReferences);
            var trackedByReferenceId = portfolio.Features
                .GroupBy(feature => feature.ReferenceId)
                .ToDictionary(group => group.Key, group => group.ToList());

            var previews = new Dictionary<string, PortfolioSourcePreview>();
            foreach (var sourceReference in sourceReferences.Distinct())
            {
                previews[sourceReference] = NarrowToPortfolio(ResolutionOf(resolutions, sourceReference), trackedByReferenceId);
            }

            return previews;
        }

        /// <summary>
        /// An answer that simply leaves a reference out says nothing about whether it still exists, so it
        /// must never read as a deletion - only a remote that answered may retire a binding.
        /// </summary>
        private static DeliverySourceResolution ResolutionOf(
            IReadOnlyDictionary<string, DeliverySourceResolution> resolutions, string sourceReference)
        {
            return resolutions.TryGetValue(sourceReference, out var resolution)
                ? resolution
                : new DeliverySourceResolution.Unavailable(DeliverySourceUnavailableReason.CapabilityWithdrawn);
        }

        private static PortfolioSourcePreview NarrowToPortfolio(
            DeliverySourceResolution resolution, Dictionary<string, List<Feature>> trackedByReferenceId)
        {
            if (resolution is not DeliverySourceResolution.Resolved resolved)
            {
                return new PortfolioSourcePreview(resolution, [], 0);
            }

            var taggedItems = resolved.Snapshot.MemberReferenceIds;
            var trackedFeatures = taggedItems
                .Where(trackedByReferenceId.ContainsKey)
                .SelectMany(referenceId => trackedByReferenceId[referenceId])
                .Distinct()
                .ToList();

            return new PortfolioSourcePreview(resolution, trackedFeatures, taggedItems.Count);
        }

        private static Dictionary<string, PortfolioSourcePreview> NothingCouldBeAsked(
            IReadOnlyList<string> sourceReferences, DeliverySourceUnavailableReason reason)
        {
            return sourceReferences
                .Distinct()
                .ToDictionary(
                    sourceReference => sourceReference,
                    _ => new PortfolioSourcePreview(new DeliverySourceResolution.Unavailable(reason), [], 0));
        }
    }
}
