using System.ComponentModel.DataAnnotations.Schema;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Delivery : IConcurrencyTokenEntity
    {
        private readonly List<Feature> features = [];

        private string name;
        private DateTime date;
        private DeliverySelectionMode selectionMode = DeliverySelectionMode.Manual;
        private string? ruleDefinitionJson;
        private int? ruleSchemaVersion;
        private string? sourceKey;
        private string? sourceReference;
        private DateTime? sourceLastSyncedOn;
        private DeliverySourceUnavailableReason? sourceUnavailableReason;

        public Delivery(string name, DateTime date, int portfolioId, DateOnly today)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
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

        // Everything archiving freezes can be given to a new Delivery but never assigned to one that
        // already exists: each has a method that refuses once the Delivery is archived. Leaving the
        // setters open would put the refusal in the hands of whoever writes the next call site.
#pragma warning disable S2292 // An auto-property is exactly what these must not be: the field has to be reachable from the methods that refuse when the Delivery is archived, while callers may only set it as the Delivery is created.
        public string Name { get => name; init => name = value; }

        public DateTime Date { get => date; init => date = value; }
#pragma warning restore S2292
        
        // Which Portfolio a Delivery belongs to is settled when it is created. Left assignable, it
        // would be the one way to move an archived Delivery somewhere else, and moving it would not
        // touch the version either, so nobody editing it at the time would be told.
        public int PortfolioId { get; init; }

        public Portfolio? Portfolio { get; init; }
        
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

#pragma warning disable S2292 // An auto-property is exactly what these must not be: the field has to be reachable from the methods that refuse when the Delivery is archived, while callers may only set it as the Delivery is created.
        public DeliverySelectionMode SelectionMode { get => selectionMode; init => selectionMode = value; }

        public string? RuleDefinitionJson { get => ruleDefinitionJson; init => ruleDefinitionJson = value; }

        public int? RuleSchemaVersion { get => ruleSchemaVersion; init => ruleSchemaVersion = value; }
#pragma warning restore S2292

#pragma warning disable S2292 // An auto-property is exactly what these must not be: releasing a Delivery from the source it follows has to clear the first two from inside the aggregate, while callers may only set them as the Delivery is created.
        public string? SourceKey { get => sourceKey; init => sourceKey = value; }

        public string? SourceReference { get => sourceReference; init => sourceReference = value; }

        public DateTime? SourceLastSyncedOn { get => sourceLastSyncedOn; init => sourceLastSyncedOn = value; }

        public DeliverySourceUnavailableReason? SourceUnavailableReason { get => sourceUnavailableReason; init => sourceUnavailableReason = value; }
#pragma warning restore S2292

        public DateTime? ArchivedOn { get; private set; }

        public void Rename(string name)
        {
            RefuseWhenArchived();
            RefuseWhenSourceBound();

            this.name = name;
            MarkAsChanged();
        }

        public void Reschedule(DateTime date)
        {
            RefuseWhenArchived();
            RefuseWhenSourceBound();

            this.date = date;
            MarkAsChanged();
        }

        public void SelectFeaturesByHand()
        {
            RefuseWhenArchived();
            RefuseWhenSourceBound();

            selectionMode = DeliverySelectionMode.Manual;
            ruleDefinitionJson = null;
            ruleSchemaVersion = null;
            MarkAsChanged();
        }

        public void SelectFeaturesByRule(string ruleDefinitionJson, int ruleSchemaVersion)
        {
            RefuseWhenArchived();
            RefuseWhenSourceBound();

            selectionMode = DeliverySelectionMode.RuleBased;
            this.ruleDefinitionJson = ruleDefinitionJson;
            this.ruleSchemaVersion = ruleSchemaVersion;
            MarkAsChanged();
        }

        public void BindToSource(string sourceKey, string sourceReference)
        {
            if (SelectionMode == DeliverySelectionMode.SourceBound)
            {
                throw DeliverySourceBoundException.AlreadyBound(Id);
            }

            selectionMode = DeliverySelectionMode.SourceBound;
            this.sourceKey = sourceKey;
            this.sourceReference = sourceReference;
            MarkAsChanged();
        }

        /// <summary>
        /// The name, the date and the Features are left exactly as the source last set them. They are
        /// the reason somebody stops following a Release rather than deleting the Delivery, so they
        /// stay and become editable again; only the trail back to the Release goes.
        /// </summary>
        public void Unbind()
        {
            if (SelectionMode != DeliverySelectionMode.SourceBound)
            {
                throw DeliverySourceBoundException.NotBound(Id);
            }

            selectionMode = DeliverySelectionMode.Manual;
            sourceKey = null;
            sourceReference = null;
            MarkAsChanged();
        }

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
            RefuseWhenArchived();
            RefuseWhenSourceBound();

            var replacement = newFeatures.ToList();

            // A rule re-match runs on every portfolio sync and usually finds exactly what it found
            // last time. Moving the token for that would expire the version an open browser is
            // holding on a timer, so archiving or saving would fail with "somebody else changed
            // this" when nobody had.
            if (AlreadyHolds(replacement))
            {
                return;
            }

            features.Clear();
            features.AddRange(replacement);
            MarkAsChanged();
        }

        private bool AlreadyHolds(List<Feature> replacement)
        {
            if (features.Count != replacement.Count)
            {
                return false;
            }

            // A Feature that has never been saved has no id yet, and several of them would collapse
            // into one another when compared by id. Nothing is claimed to be unchanged in that case.
            if (features.Exists(feature => feature.Id == 0) || replacement.Exists(feature => feature.Id == 0))
            {
                return false;
            }

            return features.Select(feature => feature.Id).ToHashSet()
                .SetEquals(replacement.Select(feature => feature.Id));
        }

        private void RefuseWhenArchived()
        {
            if (ArchivedOn is not null)
            {
                throw DeliveryArchivedException.CannotBeChanged(Id);
            }
        }

        // Choosing the Features by hand is refused along with the rest rather than taken as a request
        // to stop following the Release. Were it allowed, it would set the mode back to Manual while
        // the Delivery still named a Release, and the next refresh would go on overwriting a Delivery
        // that now claims to be somebody's to edit. Unbind is the way off, and it keeps the name, the
        // date and the Features, so nothing is lost by having to ask for it.
        private void RefuseWhenSourceBound()
        {
            if (SelectionMode == DeliverySelectionMode.SourceBound)
            {
                throw DeliverySourceBoundException.CannotBeChanged(Id);
            }
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
            // It stays because it asks the question one Feature at a time, which is how the rule is
            // stated, and because it runs first.
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
                Url = feature.Url,
            };
        }

        private static DeliveryWhenPercentile ToWhenPercentile(WhenForecast forecast, int percentile, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            var expectedDate = blackoutPeriods.ProjectWorkingDays(InstanceCalendar.AsUtcMidnight(today), forecast.GetProbability(percentile));
            return new DeliveryWhenPercentile(percentile, expectedDate, forecast.FilterApplied, forecast.ExcludedSummary);
        }
    }
}