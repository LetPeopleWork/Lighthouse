using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// What a Delivery reads as on a given day. The daily snapshot and the record pinned when a
    /// Delivery is archived are both written from this, so the two can never end up telling different
    /// stories about the same day.
    /// </summary>
    public sealed record DeliveryMetricValues
    {
        public DateTime TargetDate { get; init; }

        public int TotalWork { get; init; }

        public int DoneWork { get; init; }

        public int RemainingWork { get; init; }

        public int? EstimatedItemCount { get; init; }

        public double? LikelihoodPercentage { get; init; }

        public string? WhenDistributionJson { get; init; }

        public string? FeatureBreakdownJson { get; init; }

        public bool HasSufficientData { get; init; }

        public string? TeamsWithoutForecastJson { get; init; }

        public DeliverySelectionMode SelectionMode { get; init; }

        public string? RuleDefinitionJson { get; init; }

        public int? RuleSchemaVersion { get; init; }
    }

    /// <summary>
    /// Reads a Delivery's numbers for a day. It asks nothing of what was recorded before, so a
    /// Delivery created and closed on the same day still has a complete record to keep.
    /// </summary>
    public class DeliveryMetricValuesProjector(IBlackoutPeriodService blackoutPeriodService)
    {
        private const int CalendarHeadroomDays = 14;

        private static readonly int[] MetricPercentiles = [50, 70, 85, 95];

        private static readonly JsonSerializerOptions MetricJsonOptions = new();

        /// <summary>
        /// One lookup covers a whole set of Deliveries, so a caller reading several of them does not
        /// pay for the calendar once per Delivery.
        /// </summary>
        public IReadOnlyList<BlackoutPeriod> BlackoutPeriodsFor(IReadOnlyList<Delivery> deliveries, DateOnly today)
        {
            var windowStart = InstanceCalendar.AsUtcMidnight(today);

            return blackoutPeriodService.GetEffectiveBlackoutDays(windowStart, ForecastWindowEnd(deliveries, windowStart));
        }

        public DeliveryMetricValues Project(Delivery delivery, DateOnly today)
        {
            return Project(delivery, today, BlackoutPeriodsFor([delivery], today));
        }

        public DeliveryMetricValues Project(Delivery delivery, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var totalWork = delivery.Features.SelectMany(feature => feature.FeatureWork).Sum(work => work.TotalWorkItems);
            var remainingWork = delivery.Features.SelectMany(feature => feature.FeatureWork).Sum(work => work.RemainingWorkItems);
            var estimatedPortion = delivery.Features
                .Where(feature => feature.IsUsingDefaultFeatureSize)
                .SelectMany(feature => feature.FeatureWork)
                .Sum(work => work.TotalWorkItems);

            var metrics = delivery.CalculateMetrics(today, blackoutPeriods, MetricPercentiles);
            var hasForecast = metrics.WhenDistribution.Count > 0;
            var teamsWithoutForecast = delivery.TeamsWithoutForecast.ToList();

            return new DeliveryMetricValues
            {
                TargetDate = delivery.Date,
                TotalWork = totalWork,
                DoneWork = totalWork - remainingWork,
                RemainingWork = remainingWork,
                EstimatedItemCount = estimatedPortion > 0 ? estimatedPortion : null,
                LikelihoodPercentage = hasForecast ? metrics.LikelihoodPercentage : null,
                WhenDistributionJson = hasForecast
                    ? JsonSerializer.Serialize(
                        metrics.WhenDistribution.Select(point => new { Probability = (double)point.Percentile, point.ExpectedDate, point.BeyondHorizon, point.HorizonYears }),
                        MetricJsonOptions)
                    : null,
                FeatureBreakdownJson = metrics.FeatureBreakdown.Count > 0
                    ? JsonSerializer.Serialize(metrics.FeatureBreakdown, MetricJsonOptions)
                    : null,
                HasSufficientData = metrics.HasSufficientData,
                TeamsWithoutForecastJson = teamsWithoutForecast.Count > 0
                    ? JsonSerializer.Serialize(teamsWithoutForecast, MetricJsonOptions)
                    : null,
                SelectionMode = delivery.SelectionMode,
                RuleDefinitionJson = delivery.RuleDefinitionJson,
                RuleSchemaVersion = delivery.RuleSchemaVersion,
            };
        }

        private static DateTime ForecastWindowEnd(IReadOnlyList<Delivery> deliveries, DateTime today)
        {
            var latestDeliveryDate = deliveries.Count == 0
                ? today
                : deliveries.Max(delivery => delivery.Date.Date);

            var horizon = latestDeliveryDate > today ? latestDeliveryDate : today;
            var aroundTargetDates = horizon.AddDays(CalendarHeadroomDays);

            // A Delivery's dates run as far as its Features' forecasts do, which can be well past the
            // target date. Recurring rules are only expanded inside the window, so a shorter one would
            // count every weekend beyond it as a working day.
            var reachOfTheForecasts = FeatureForecastWindow.EndFor(today, deliveries.SelectMany(delivery => delivery.Features));

            // Across Teams a Delivery is done only when every Team is, so its own percentiles can lie
            // well beyond those of any single Feature.
            var reachOfTheDeliveries = deliveries
                .Select(delivery => FeatureForecastWindow.EndFor(today, DeliveryCompletionForecast.Build(delivery.Features.ToList())))
                .DefaultIfEmpty(today)
                .Max();

            return new[] { aroundTargetDates, reachOfTheForecasts, reachOfTheDeliveries }.Max();
        }
    }
}
