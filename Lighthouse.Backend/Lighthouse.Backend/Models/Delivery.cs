using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Delivery : IConcurrencyTokenEntity
    {
        public Delivery(string name, DateTime date, int portfolioId, DateOnly today)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (DateOnly.FromDateTime(date) <= today)
            {
                throw new ArgumentException("Delivery date must be in the future");
            }

            Name = name;
            Date = date;
            PortfolioId = portfolioId;
        }

        public Delivery()
        {
            Name = string.Empty;
        }

        public int Id { get; set; }

        public Guid ConcurrencyToken { get; set; }

        public string Name { get; set; }
        
        public DateTime Date { get; set; }
        
        public int PortfolioId { get; set; }
        
        public Portfolio? Portfolio { get; set; }
        
        public List<Feature> Features { get; } = [];

        public DeliverySelectionMode SelectionMode { get; set; } = DeliverySelectionMode.Manual;

        public string? RuleDefinitionJson { get; set; }

        public int? RuleSchemaVersion { get; set; }

        public DeliveryMetricsProjection CalculateMetrics(DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods, params int[] percentiles)
        {
            var featureBreakdown = CalculateFeatureBreakdown(today, blackoutPeriods);

            if (Features.Count == 0)
            {
                return new DeliveryMetricsProjection(0.0, [], featureBreakdown);
            }

            // One un-forecastable feature makes the whole delivery un-forecastable - reporting a number
            // for the rest would quietly ignore work that must still happen (ADR-112 D8).
            //
            // Stryker disable once all: since the row-set guard below also rejects a missing or
            // zero-trial row, the two detect the same condition and mutating this one is unobservable.
            // It stays because it carries the ADR-112 semantic at feature grain and runs first.
            if (Features.Any(feature => !feature.CanBeForecast))
            {
                return new DeliveryMetricsProjection(null, [], featureBreakdown);
            }

            // Stryker disable once all: equivalent while Bug #5586 stands. Skipping this guard falls
            // through to an empty aggregate, whose GetLikelihood returns 100 from the trialCounter == 0
            // branch and whose GetProbability returns today - the same answer by luck. The guard is what
            // stops the delivery depending on that branch, and becomes observable when #5586 is fixed.
            if (!HasRemainingWork())
            {
                return new DeliveryMetricsProjection(100.0, PercentilesOf(DayZeroMarker, today, blackoutPeriods, percentiles), featureBreakdown);
            }

            // Backstop at pair grain: guard 2 already covers this once Feature.TeamsWithoutForecast can
            // name the team, but this is the one place that re-derives the predicate from the row set
            // the maths actually consumes, so the two cannot drift apart silently (ADR-113 DDD-7).
            var deliveryForecast = DeliveryCompletionForecast.Build(Features);

            if (deliveryForecast == null)
            {
                return new DeliveryMetricsProjection(null, [], featureBreakdown);
            }

            return new DeliveryMetricsProjection(
                LikelihoodOf(deliveryForecast, today, blackoutPeriods),
                PercentilesOf(deliveryForecast, today, blackoutPeriods, percentiles),
                featureBreakdown);
        }

        // ForecastService emits this shape for a feature with nothing left to do, so a delivery whose
        // work is all done reports today rather than a stale future date (ADR-113 DDD-9).
        private static WhenForecast DayZeroMarker => new(new Dictionary<int, int> { { 0, 0 } });

        private bool HasRemainingWork()
        {
            // The same predicate the row set uses, per pair rather than summed, so the two cannot
            // disagree about what "nothing left to do" means.
            return Features.Exists(feature => feature.FeatureWork.Exists(work => work.RemainingWorkItems > 0));
        }

        private double LikelihoodOf(WhenForecast forecast, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            // Mirrors Feature.GetLikelhoodForDate's `date != default` short-circuit, which the delivery
            // used to inherit through the deleted governing-feature call. Reachable only through the EF
            // parameterless constructor.
            if (Date == default)
            {
                return 100;
            }

            return forecast.GetLikelihood(blackoutPeriods.CountWorkingDays(InstanceCalendar.AsUtcMidnight(today), Date));
        }

        private static List<DeliveryWhenPercentile> PercentilesOf(WhenForecast forecast, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods, int[] percentiles)
        {
            return percentiles
                .Select(percentile => ToWhenPercentile(forecast, percentile, today, blackoutPeriods))
                .ToList();
        }

        private List<DeliveryFeatureMetric> CalculateFeatureBreakdown(DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            return Features
                .Where(feature => feature.FeatureWork.Sum(work => work.TotalWorkItems) > 0)
                .Select(feature => ToFeatureMetric(feature, today, blackoutPeriods))
                .ToList();
        }

        private DeliveryFeatureMetric ToFeatureMetric(Feature feature, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var totalItems = feature.FeatureWork.Sum(work => work.TotalWorkItems);
            var remainingItems = feature.FeatureWork.Sum(work => work.RemainingWorkItems);
            var completion = Math.Clamp((double)(totalItems - remainingItems) / totalItems * 100.0, 0.0, 100.0);

            return new DeliveryFeatureMetric(feature.ReferenceId, feature.Name, completion, feature.GetLikelhoodForDate(Date, today, blackoutPeriods));
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var expectedDate = blackoutPeriods.ProjectWorkingDays(InstanceCalendar.AsUtcMidnight(today), forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}