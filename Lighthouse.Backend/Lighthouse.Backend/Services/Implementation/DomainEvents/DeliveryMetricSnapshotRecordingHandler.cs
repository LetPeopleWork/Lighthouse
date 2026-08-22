using System.Diagnostics;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class DeliveryMetricSnapshotRecordingHandler(
        IDeliveryRepository deliveryRepository,
        IDeliveryMetricSnapshotRepository snapshotRepository,
        DeliveryMetricValuesProjector projector,
        ILighthouseClock clock,
        ILogger<DeliveryMetricSnapshotRecordingHandler> logger) : IDomainEventHandler<PortfolioForecastsUpdated>
    {
        public async Task HandleAsync(PortfolioForecastsUpdated domainEvent, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var deliveries = deliveryRepository.GetByPortfolioAsync(domainEvent.PortfolioId).ToList();

                // Bug #5567: the recorded day, the forecast window start and the snapshot key are
                // all the same instance calendar day, read once.
                var today = clock.Today;
                var blackoutPeriods = projector.BlackoutPeriodsFor(deliveries, today);

                foreach (var delivery in deliveries)
                {
                    var values = projector.Project(delivery, today, blackoutPeriods);

                    var snapshot = snapshotRepository.GetOrCreateForDay(delivery.Id, today);
                    snapshot.TargetDateAtSnapshot = values.TargetDate;
                    snapshot.TotalWork = values.TotalWork;
                    snapshot.DoneWork = values.DoneWork;
                    snapshot.RemainingWork = values.RemainingWork;
                    snapshot.EstimatedItemCount = values.EstimatedItemCount;
                    snapshot.LikelihoodPercentage = values.LikelihoodPercentage;
                    snapshot.WhenDistributionJson = values.WhenDistributionJson;
                    snapshot.FeatureBreakdownJson = values.FeatureBreakdownJson;
                }

                await snapshotRepository.Save();

                stopwatch.Stop();
                logger.LogDebug(
                    "Recorded delivery metric snapshots for Portfolio {PortfolioId}: {SnapshotCount} deliveries in {ElapsedMilliseconds}ms",
                    domainEvent.PortfolioId, deliveries.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                logger.LogError(
                    exception,
                    "Failed to record delivery metric snapshots for Portfolio {PortfolioId} after {ElapsedMilliseconds}ms; snapshot recording is best-effort and the next forecast update will retry",
                    domainEvent.PortfolioId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
