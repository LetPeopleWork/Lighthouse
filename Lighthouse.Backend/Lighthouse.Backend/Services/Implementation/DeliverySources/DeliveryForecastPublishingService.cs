using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.DeliverySources
{
    public class DeliveryForecastPublishingService(
        IWorkTrackingConnectorFactory workTrackingConnectorFactory,
        IDeliveryForecastBlockRenderer renderer,
        DeliveryMetricValuesProjector projector,
        ILighthouseClock clock,
        ILogger<DeliveryForecastPublishingService> logger) : IDeliveryForecastPublishingService
    {
        /// <summary>
        /// The three the product's own screen shows. Read from here rather than restated at the block,
        /// so the Release and the Lighthouse page cannot come to disagree about which ones are on show.
        /// </summary>
        private static readonly int[] ThePercentilesTheProductShows = [70, 85, 95];

        public async Task PublishForPortfolio(Portfolio portfolio, RecordableDeliveries deliveries)
        {
            ArgumentNullException.ThrowIfNull(portfolio);
            ArgumentNullException.ThrowIfNull(deliveries);

            var broadcasting = deliveries.Where(WantsItsForecastBroadcast).ToList();

            if (broadcasting.Count == 0)
            {
                return;
            }

            if (PublisherBehind(portfolio) is not { } publisher)
            {
                return;
            }

            // One calendar lookup for the whole set, and one day for every Delivery in it: two
            // Deliveries published in the same pass must not be dated from two different readings of
            // the clock, or a run crossing midnight would stamp them differently.
            var today = clock.Today;
            var blackoutPeriods = projector.BlackoutPeriodsFor(broadcasting, today);

            foreach (var delivery in broadcasting)
            {
                await PublishOne(portfolio, publisher, delivery, today, blackoutPeriods);
            }
        }

        /// <summary>
        /// Whether this Delivery is one to broadcast. Being heard from is asked for explicitly rather
        /// than inferred from nothing being wrong: the reason a source is finished is absent both on a
        /// Delivery whose Release is healthy and on one that nothing has ever resolved, and only the
        /// first of those has a reference anybody has confirmed exists.
        /// </summary>
        private static bool WantsItsForecastBroadcast(Delivery delivery)
        {
            return delivery.PublishForecastToSource
                && delivery.SelectionMode == DeliverySelectionMode.SourceBound
                && !string.IsNullOrEmpty(delivery.SourceKey)
                && !string.IsNullOrEmpty(delivery.SourceReference)
                && delivery.SourceLastSyncedOn is not null
                && delivery.SourceUnavailableReason is null;
        }

        /// <summary>
        /// What can write to this connection, or nothing. Reading and writing are two capabilities, so a
        /// connection that reads Releases perfectly well may still answer no here - which is the state
        /// the refusal report exists to surface.
        ///
        /// Resolving the connector reaches the same credential the read does and can throw for the same
        /// reasons. This pass hangs off the forecast, which carries every number on the Portfolio, so
        /// letting that throw escape would lose all of them over one connection nobody can read.
        /// </summary>
        private IDeliveryForecastPublisher? PublisherBehind(Portfolio portfolio)
        {
            var connection = portfolio.WorkTrackingSystemConnection;

            try
            {
                return workTrackingConnectorFactory.GetWorkTrackingConnector(connection.WorkTrackingSystem) is IDeliveryForecastPublisher publisher
                    && publisher.SupportsDeliveryForecastPublishing(connection)
                        ? publisher
                        : null;
            }
#pragma warning disable CA1031 // asking what a connection can write must cost no more than asking what it can read
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // Stryker disable once all: diagnostic log text is not behaviour. Publishing nothing at
                // all is, and that is asserted.
                logger.LogWarning(
                    exception,
                    "Could not establish whether the connection behind Portfolio {PortfolioName} may publish a forecast; nothing is published this round",
                    portfolio.Name);

                return null;
            }
        }

        private async Task PublishOne(
            Portfolio portfolio,
            IDeliveryForecastPublisher publisher,
            Delivery delivery,
            DateOnly today,
            IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            if (WhatThereIsToSay(delivery, today, blackoutPeriods) is not { } block)
            {
                // Stryker disable once all: diagnostic log text is not behaviour. Writing nothing is,
                // and that is asserted.
                logger.LogDebug(
                    "Delivery {DeliveryId} has no forecast to publish yet; its Release is left as it is",
                    delivery.Id);

                return;
            }

            var publication = new DeliveryForecastPublication(delivery.SourceKey!, delivery.SourceReference!, renderer.Render(block));

            var result = await WriteWithoutTakingTheRoundDown(portfolio, publisher, delivery, publication);

            if (result is null)
            {
                return;
            }

            Record(delivery, result);
        }

        /// <summary>
        /// The four things the block states, or nothing when the Delivery has no forecast to state. A
        /// Delivery nobody can forecast has none of the three dates and no likelihood either, and a block
        /// with those lines left blank would say less than no block at all - so it is skipped and
        /// whatever was published last stays, carrying the date that says how old it is.
        /// </summary>
        private static DeliveryForecastBlock? WhatThereIsToSay(
            Delivery delivery, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var metrics = delivery.CalculateMetrics(today, blackoutPeriods, ThePercentilesTheProductShows);

            if (metrics.LikelihoodPercentage is not { } likelihood || metrics.WhenDistribution.Count == 0)
            {
                return null;
            }

            return new DeliveryForecastBlock(
                today,
                [.. metrics.WhenDistribution.Select(percentile =>
                    new DeliveryForecastBlockPercentile(percentile.Percentile, DateOnly.FromDateTime(percentile.ExpectedDate)))],
                DateOnly.FromDateTime(delivery.Date),
                likelihood);
        }

        /// <summary>
        /// A remote that could not be written to has said nothing about whether the Release is still
        /// there, so the Delivery keeps everything it has and the Deliveries beside it are still
        /// published. Reported as nothing rather than as a refusal: a refusal is an answer, and this is
        /// the absence of one.
        /// </summary>
        private async Task<DeliveryForecastPublishResult?> WriteWithoutTakingTheRoundDown(
            Portfolio portfolio,
            IDeliveryForecastPublisher publisher,
            Delivery delivery,
            DeliveryForecastPublication publication)
        {
            try
            {
                return await publisher.PublishAsync(portfolio.WorkTrackingSystemConnection, publication);
            }
#pragma warning disable CA1031 // a source that could not be written to costs its own Delivery this round and nothing else
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // Stryker disable once all: diagnostic log text is not behaviour. That this Delivery is
                // left as it stands while the ones beside it are still published is the claim, and it is
                // asserted.
                logger.LogWarning(
                    exception,
                    "The forecast of Delivery {DeliveryId} could not be written to the source it follows; it keeps everything it has and the next round asks again",
                    delivery.Id);

                return null;
            }
        }

        /// <summary>
        /// What each answer means for the Delivery. A source that is not there is the same finding a
        /// failed read makes and raises the same state, because it is the same fact about the same
        /// Release. A refusal is deliberately not that: it says the credential may not write, which is
        /// about the connection rather than about the Release, and treating it as a deleted Release
        /// would send an administrator to re-create something that never moved.
        /// </summary>
        private void Record(Delivery delivery, DeliveryForecastPublishResult result)
        {
            switch (result)
            {
                case DeliveryForecastPublishResult.TargetMissing:
                    delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);
                    break;

                case DeliveryForecastPublishResult.Refused refused:
                    // Stryker disable once all: diagnostic log text is not behaviour, and where the
                    // refusal is written down is slice 05's subject rather than this one's.
                    logger.LogWarning(
                        "The source Delivery {DeliveryId} follows refused the forecast: {Reason}",
                        delivery.Id,
                        refused.Reason);
                    break;

                default:
                    // Stryker disable once all: diagnostic log text is not behaviour.
                    logger.LogDebug("Published the forecast of Delivery {DeliveryId} to the source it follows", delivery.Id);
                    break;
            }
        }
    }
}
