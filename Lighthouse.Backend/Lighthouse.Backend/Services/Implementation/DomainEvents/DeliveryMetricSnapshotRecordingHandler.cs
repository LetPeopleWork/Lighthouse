using System.Diagnostics;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class DeliveryMetricSnapshotRecordingHandler(
        IDeliveryRepository deliveryRepository,
        IDeliveryMetricSnapshotRepository snapshotRepository,
        IBlackoutPeriodService blackoutPeriodService,
        ILighthouseClock clock,
        ILogger<DeliveryMetricSnapshotRecordingHandler> logger) : IDomainEventHandler<PortfolioForecastsUpdated>
    {
        private static readonly int[] SnapshotPercentiles = [50, 70, 85, 95];

        private static readonly JsonSerializerOptions WhenDistributionJsonOptions = new();

        public async Task HandleAsync(PortfolioForecastsUpdated domainEvent, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var deliveries = deliveryRepository.GetByPortfolioAsync(domainEvent.PortfolioId).ToList();

                // Bug #5567: the recorded day, the forecast window start and the snapshot key are all
                // the SAME instance calendar day, read once. Nothing on this path reduces an instant
                // to a day any more - the day travels as a DateOnly and the DateTime the blackout
                // service still speaks is that day's UTC midnight, so the global EF value converter
                // has nothing left to shift.
                var today = clock.Today;
                var forecastWindowStart = clock.TodayAsUtcMidnight;
                var blackoutPeriods = blackoutPeriodService.GetEffectiveBlackoutDays(
                    forecastWindowStart, ForecastWindowEnd(deliveries, forecastWindowStart));

                foreach (var delivery in deliveries)
                {
                    var totalWork = delivery.Features.SelectMany(feature => feature.FeatureWork).Sum(work => work.TotalWorkItems);
                    var remainingWork = delivery.Features.SelectMany(feature => feature.FeatureWork).Sum(work => work.RemainingWorkItems);
                    var estimatedPortion = delivery.Features
                        .Where(feature => feature.IsUsingDefaultFeatureSize)
                        .SelectMany(feature => feature.FeatureWork)
                        .Sum(work => work.TotalWorkItems);

                    var snapshot = snapshotRepository.GetOrCreateForDay(delivery.Id, today);
                    snapshot.TargetDateAtSnapshot = delivery.Date;
                    snapshot.TotalWork = totalWork;
                    snapshot.DoneWork = totalWork - remainingWork;
                    snapshot.RemainingWork = remainingWork;
                    snapshot.EstimatedItemCount = estimatedPortion > 0 ? estimatedPortion : null;

                    var metrics = delivery.CalculateMetrics(today, blackoutPeriods, SnapshotPercentiles);
                    var hasForecast = metrics.WhenDistribution.Count > 0;
                    snapshot.LikelihoodPercentage = hasForecast ? metrics.LikelihoodPercentage : null;
                    snapshot.WhenDistributionJson = hasForecast
                        ? JsonSerializer.Serialize(
                            metrics.WhenDistribution.Select(point => new { Probability = (double)point.Percentile, point.ExpectedDate }),
                            WhenDistributionJsonOptions)
                        : null;
                    snapshot.FeatureBreakdownJson = metrics.FeatureBreakdown.Count > 0
                        ? JsonSerializer.Serialize(metrics.FeatureBreakdown, WhenDistributionJsonOptions)
                        : null;
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

        private static DateTime ForecastWindowEnd(List<Delivery> deliveries, DateTime today)
        {
            const int CalendarHeadroomDays = 14;

            var latestDeliveryDate = deliveries.Count == 0
                ? today
                : deliveries.Max(delivery => delivery.Date.Date);

            var horizon = latestDeliveryDate > today ? latestDeliveryDate : today;

            return horizon.AddDays(CalendarHeadroomDays);
        }
    }
}
