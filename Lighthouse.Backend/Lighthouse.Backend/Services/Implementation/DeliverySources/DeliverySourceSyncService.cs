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

        /// <summary>
        /// Asked whether the connection still offers this kind of source before it is asked anything
        /// about one. A connection that has stopped offering it answers a read by throwing, and a throw
        /// is indistinguishable from the remote being briefly unreachable - so without this the one
        /// permanent failure that is about the connection would arrive looking exactly like a blip, and
        /// slice 03 could never tell a Delivery whose source is finished from one whose source is down.
        /// </summary>
        private async Task ResyncEverythingOnOneSource(Portfolio portfolio, string sourceKey, IReadOnlyList<Delivery> deliveries)
        {
            if (!resolver.OffersSource(portfolio, sourceKey))
            {
                // Stryker disable once all: diagnostic log text is not behaviour. What this branch does
                // is flag every Delivery below and ask the remote nothing, and both are asserted.
                logger.LogWarning(
                    "The connection behind Portfolio {PortfolioName} no longer offers a source called {SourceKey}; the {DeliveryCount} Deliveries following one keep the values they already have",
                    portfolio.Name,
                    sourceKey,
                    deliveries.Count);

                foreach (var delivery in deliveries)
                {
                    SayTheSourceIsFinished(delivery, DeliverySourceUnavailableReason.CapabilityWithdrawn);
                }

                return;
            }

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
                // Stryker disable once all: diagnostic log text is not behaviour. What this branch does
                // is return null so the Deliveries keep their values, and that is asserted; the sentence
                // an operator reads about it is not something a test should be pinned to.
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
        /// The transition table. A source that came back alive writes its values and takes any standing
        /// notice off; a source that resolved to nothing keeps the values and says which way it is
        /// finished; a source that could not be read says nothing at all, because a bad minute at the
        /// remote must never read as a Release somebody deleted.
        ///
        /// A reference the answer did not mention arrives as a read failure and is left alone for the
        /// same reason: an answer that omits something has said nothing about whether it exists.
        /// </summary>
        private void ApplyWhatCameBack(Delivery delivery, IReadOnlyDictionary<string, PortfolioSourcePreview> previews)
        {
            if (!previews.TryGetValue(delivery.SourceReference!, out var preview))
            {
                return;
            }

            if (preview.Resolution is not DeliverySourceResolution.Resolved resolved)
            {
                if (WhatItIsFinishedBy(preview.Resolution) is { } reason)
                {
                    SayTheSourceIsFinished(delivery, reason);
                }

                return;
            }

            try
            {
                // The day rather than the instant, and that is what keeps a refresh that found nothing
                // new out of the database entirely: writing the same value back is not a change, so the
                // row joins the save at most once a day instead of on every refresh. It matters because
                // the save is one transaction over every Delivery of the Portfolio and it is dropped
                // whole if any row's version has moved - a Delivery in the write set for no reason is a
                // Delivery that can cost every other one its refresh.
                delivery.SyncFromSource(
                    resolved.Snapshot.Name, resolved.Snapshot.Date, preview.TrackedFeatures, clock.TodayAsUtcMidnight);
            }
            // Only what a remote answer can be wrong about. The aggregate's own refusals are not caught:
            // this pass hands it only Deliveries that follow a source and that nobody has retired, so a
            // refusal means that stopped being true - and swallowing it would leave the sync silently
            // doing nothing at all, one log line per Delivery per refresh, with the refresh still
            // reporting success.
            catch (ArgumentException exception)
            {
                // Stryker disable once all: diagnostic log text is not behaviour. That this Delivery is
                // left on its values while the ones beside it still sync is the claim, and it is
                // asserted; the wording of the line about it is not.
                logger.LogWarning(
                    exception,
                    "Delivery {DeliveryId} refused what its source now says and keeps the values it already has",
                    delivery.Id);
            }
        }

        /// <summary>
        /// Which verdicts mean the binding is finished, and which mean only that today's attempt told us
        /// nothing. The read-failure reason is the one that maps to nothing at all - the aggregate
        /// refuses it outright, so this returning it would be a crash rather than a wrong flag, but it
        /// is filtered here because leaving the Delivery alone is the behaviour, not an error.
        /// </summary>
        private static DeliverySourceUnavailableReason? WhatItIsFinishedBy(DeliverySourceResolution resolution)
        {
            return resolution switch
            {
                DeliverySourceResolution.NotFound => DeliverySourceUnavailableReason.SourceNotFound,
                DeliverySourceResolution.NoDate => DeliverySourceUnavailableReason.SourceHasNoDate,
                DeliverySourceResolution.Unavailable unavailable
                    when unavailable.Reason != DeliverySourceUnavailableReason.SourceReadFailed
                    => unavailable.Reason,
                _ => null,
            };
        }

        private void SayTheSourceIsFinished(Delivery delivery, DeliverySourceUnavailableReason reason)
        {
            try
            {
                delivery.MarkSourceUnavailable(reason);
            }
            catch (ArgumentException exception)
            {
                // Stryker disable once all: diagnostic log text is not behaviour. The claim is that one
                // Delivery refusing a verdict costs no other Delivery its refresh, which is asserted.
                logger.LogWarning(
                    exception,
                    "Delivery {DeliveryId} refused the verdict its source came back with and keeps the values it already has",
                    delivery.Id);
            }
        }
    }
}
