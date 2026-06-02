using System.Diagnostics;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class DeliveryMetricSnapshotRecordingHandler(
        IDeliveryRepository deliveryRepository,
        IDeliveryMetricSnapshotRepository snapshotRepository,
        ILogger<DeliveryMetricSnapshotRecordingHandler> logger) : IDomainEventHandler<PortfolioForecastsUpdated>
    {
        public async Task HandleAsync(PortfolioForecastsUpdated domainEvent, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var deliveries = deliveryRepository.GetByPortfolioAsync(domainEvent.PortfolioId).ToList();
                var recordedAt = DateTime.UtcNow;

                foreach (var delivery in deliveries)
                {
                    var totalWork = delivery.Features.SelectMany(feature => feature.FeatureWork).Sum(work => work.TotalWorkItems);
                    var remainingWork = delivery.Features.SelectMany(feature => feature.FeatureWork).Sum(work => work.RemainingWorkItems);

                    var snapshot = snapshotRepository.GetOrCreateForDay(delivery.Id, recordedAt);
                    snapshot.TotalWork = totalWork;
                    snapshot.DoneWork = totalWork - remainingWork;
                    snapshot.RemainingWork = remainingWork;
                }

                await snapshotRepository.Save();

                stopwatch.Stop();
                logger.LogInformation(
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
