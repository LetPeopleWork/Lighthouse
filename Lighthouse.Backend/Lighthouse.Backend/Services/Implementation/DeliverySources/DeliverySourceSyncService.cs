using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DeliverySources
{
    public class DeliverySourceSyncService(
        IDeliverySourceResolver resolver,
        ILighthouseClock clock,
        ILogger<DeliverySourceSyncService> logger) : IDeliverySourceSyncService
    {
        public async Task ResyncSourceBoundDeliveries(Portfolio portfolio, RecordableDeliveries deliveries)
        {
            ArgumentNullException.ThrowIfNull(portfolio);
            ArgumentNullException.ThrowIfNull(deliveries);

            var followingASource = deliveries
                .Where(delivery => delivery.SelectionMode == DeliverySelectionMode.SourceBound
                    && !string.IsNullOrEmpty(delivery.SourceKey)
                    && !string.IsNullOrEmpty(delivery.SourceReference))
                .ToList();

            foreach (var onOneSource in followingASource.GroupBy(delivery => delivery.SourceKey!))
            {
                await ResyncEverythingOnOneSource(portfolio, onOneSource.Key, [.. onOneSource]);
            }
        }

        private async Task ResyncEverythingOnOneSource(Portfolio portfolio, string sourceKey, IReadOnlyList<Delivery> deliveries)
        {
            var previews = await AskTheSourceWithoutTakingTheRefreshDown(portfolio, sourceKey, deliveries);

            if (previews is null)
            {
                return;
            }

            foreach (var delivery in deliveries)
            {
                ApplyWhatCameBack(delivery, previews);
            }
        }

        /// <summary>
        /// A connection that has stopped offering the source throws rather than answering it, and a
        /// credential that can no longer be read throws before the remote is reached at all. This pass
        /// runs inside the Portfolio refresh, which carries every other number on the Portfolio, so
        /// letting either reach the refresh would lose all of them over one source nobody can read.
        /// </summary>
        private async Task<IReadOnlyDictionary<string, PortfolioSourcePreview>?> AskTheSourceWithoutTakingTheRefreshDown(
            Portfolio portfolio, string sourceKey, IReadOnlyList<Delivery> deliveries)
        {
            var references = deliveries.Select(delivery => delivery.SourceReference!).Distinct().ToList();

            try
            {
                return await resolver.ResolveForPortfolio(portfolio, sourceKey, references);
            }
#pragma warning disable CA1031 // an unreadable source costs its own Deliveries their refresh and nothing else; the next refresh asks again
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogWarning(
                    exception,
                    "Source {SourceKey} of Portfolio {PortfolioName} could not be read; the {DeliveryCount} Deliveries following it keep the values they already have",
                    sourceKey,
                    portfolio.Name,
                    deliveries.Count);

                return null;
            }
        }

        /// <summary>
        /// Only a source that resolved to a live object writes anything. A read that failed and a
        /// source that resolved to nothing both leave the Delivery exactly as it stands - telling the
        /// two apart, and saying so on screen, is what slice 03 adds on top of this.
        /// </summary>
        private void ApplyWhatCameBack(Delivery delivery, IReadOnlyDictionary<string, PortfolioSourcePreview> previews)
        {
            if (!previews.TryGetValue(delivery.SourceReference!, out var preview)
                || preview.Resolution is not DeliverySourceResolution.Resolved resolved)
            {
                return;
            }

            try
            {
                delivery.SyncFromSource(
                    resolved.Snapshot.Name, resolved.Snapshot.Date, preview.TrackedFeatures, clock.Now.UtcDateTime);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                logger.LogWarning(
                    exception,
                    "Delivery {DeliveryId} refused what its source now says and keeps the values it already has",
                    delivery.Id);
            }
        }
    }
}
