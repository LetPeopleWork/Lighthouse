using System.ComponentModel.DataAnnotations.Schema;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Delivery : IConcurrencyTokenEntity
    {
        private readonly List<Feature> features = [];

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
        
        // Handed out read-only so that archiving a Delivery is the end of its Feature set, not merely
        // an instruction not to touch it. Every write goes through ReplaceFeatures, which is the one
        // place that can refuse.
        public IReadOnlyList<Feature> Features => features;

        // Named here rather than at each reader, so what an archived Delivery says about a team it
        // was waiting on is the same sentence a live one says.
        [NotMapped]
        public IEnumerable<string> TeamsWithoutForecast => features
            .SelectMany(feature => feature.TeamsWithoutForecast)
            .Select(team => team.Name)
            .Distinct()
            .Order();

        public DeliverySelectionMode SelectionMode { get; set; } = DeliverySelectionMode.Manual;

        public string? RuleDefinitionJson { get; set; }

        public int? RuleSchemaVersion { get; set; }

        public DateTime? ArchivedOn { get; private set; }

        public void Archive(DateTime archivedOn)
        {
            if (ArchivedOn is not null)
            {
                throw DeliveryArchivedException.AlreadyArchived(Id);
            }

            ArchivedOn = archivedOn;
            MarkAsChanged();
        }

        public void Unarchive()
        {
            if (ArchivedOn is null)
            {
                throw DeliveryArchivedException.NotArchived(Id);
            }

            ArchivedOn = null;
            MarkAsChanged();
        }

        public void ReplaceFeatures(IEnumerable<Feature> newFeatures)
        {
            if (ArchivedOn is not null)
            {
                throw DeliveryArchivedException.CannotBeChanged(Id);
            }

            features.Clear();
            features.AddRange(newFeatures);
            MarkAsChanged();
        }

        /// <summary>
        /// Which Features a Delivery contains is part of what the Delivery is, but the database keeps
        /// that in a table of its own - so changing only the Feature set writes no row for the
        /// Delivery itself, and optimistic concurrency, which works by comparing that row, never gets
        /// a chance to notice. Moving the token by hand puts the row back in the write, which is what
        /// makes two people changing the same Delivery at once a conflict rather than a silent
        /// last-one-wins.
        /// </summary>
        private void MarkAsChanged()
        {
            ConcurrencyToken = Guid.NewGuid();
        }

        public DeliveryMetricsProjection CalculateMetrics(DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods, params int[] percentiles)
        {
            var featureBreakdown = CalculateFeatureBreakdown(today, blackoutPeriods);
            var hasSufficientData = HasSufficientDataAcrossContributingFeatures();

            if (Features.Count == 0)
            {
                return new DeliveryMetricsProjection(0.0, [], featureBreakdown, hasSufficientData);
            }

            // One un-forecastable feature makes the whole delivery un-forecastable - reporting a number
            // for the rest would quietly ignore work that must still happen.
            //
            // Stryker disable once all: since the row-set guard below also rejects a missing or
            // zero-trial row, the two detect the same condition and mutating this one is unobservable.
            // It stays because it carries the ADR-112 semantic at feature grain and runs first.
            if (Features.Any(feature => !feature.CanBeForecast))
            {
                return new DeliveryMetricsProjection(null, [], featureBreakdown, hasSufficientData);
            }

            if (!HasRemainingWork())
            {
                return new DeliveryMetricsProjection(100.0, PercentilesOf(DayZeroMarker, today, blackoutPeriods, percentiles), featureBreakdown, hasSufficientData);
            }

            // The check above answers the same question a feature at a time, but this is the one place
            // that asks it of the rows the arithmetic actually consumes. Keeping both means the two
            // cannot come to disagree about what can be forecast without something failing loudly.
            var deliveryForecast = DeliveryCompletionForecast.Build(features);

            if (deliveryForecast == null)
            {
                return new DeliveryMetricsProjection(null, [], featureBreakdown, hasSufficientData);
            }

            return new DeliveryMetricsProjection(
                LikelihoodOf(deliveryForecast, today, blackoutPeriods),
                PercentilesOf(deliveryForecast, today, blackoutPeriods, percentiles),
                featureBreakdown,
                hasSufficientData);
        }

        // ForecastService emits this shape for a feature with nothing left to do, so a delivery whose
        // work is all done reports today rather than a stale future date.
        private static WhenForecast DayZeroMarker => new(new Dictionary<int, int> { { 0, 0 } });

        private bool HasRemainingWork()
        {
            return features.Exists(StillHasWork);
        }

        // Features with nothing left to do are left out on purpose. A finished feature carries the
        // day-0 marker ForecastService emits, which names no team, and the flag never gets set on it -
        // so counting it would report "not enough history" about a feature nobody ever asked about.
        private bool HasSufficientDataAcrossContributingFeatures()
        {
            return Features.Where(StillHasWork).All(RestsOnEnoughHistory);
        }

        // Per pair rather than summed, so this cannot disagree with the row set about what "nothing
        // left to do" means.
        private static bool StillHasWork(Feature feature)
        {
            return feature.FeatureWork.Exists(work => work.RemainingWorkItems > 0);
        }

        // The same answer AggregatedWhenForecast gives, without paying to rebuild it.
        private static bool RestsOnEnoughHistory(Feature feature)
        {
            return feature.Forecasts.TrueForAll(forecast => forecast.HasSufficientData);
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

            return new DeliveryFeatureMetric(feature.ReferenceId, feature.Name, completion, feature.GetLikelhoodForDate(Date, today, blackoutPeriods))
            {
                TotalItems = totalItems,
                IsUsingDefaultSize = feature.IsUsingDefaultFeatureSize,
            };
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var expectedDate = blackoutPeriods.ProjectWorkingDays(InstanceCalendar.AsUtcMidnight(today), forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}