﻿using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lighthouse.Backend.Models
{
    public class Feature : WorkItemBase
    {
        private readonly List<FeatureDependencyReference> dependsOnReferences = [];

        public Feature() : this(new List<(Team team, int remainingItems, int totalItems)>())
        {
        }

        public Feature(WorkItemBase workItemBase) : this(workItemBase, [])
        {
        }

        /// <summary>
        /// A Feature a connector builds field by field rather than from a work item it already has, links
        /// and all. Same reason as the constructor below: the links are known before the Feature has a
        /// row, so taking them here keeps the rewrite of what an existing Feature waits on to the
        /// reconciler alone.
        /// </summary>
        public Feature(IEnumerable<FeatureDependencyReference> dependsOn)
        {
            dependsOnReferences.AddRange(dependsOn);
        }

        /// <summary>
        /// A Feature built straight from what a work tracking system just handed over, links and all. A
        /// connector reads those links off a work item that has no row here yet, so it has nowhere to put
        /// them; taking them at construction means it never needs the rewrite that changes what a Feature
        /// already on file waits on, and that rewrite therefore stays the reconciler's alone.
        /// </summary>
        public Feature(WorkItemBase workItemBase, IEnumerable<FeatureDependencyReference> dependsOn) : base(workItemBase)
        {
            dependsOnReferences.AddRange(dependsOn);
        }

        public Feature(Team team, int remainingItems) : this([(team, remainingItems, remainingItems)])
        {
        }

        public Feature(IEnumerable<(Team team, int remainingItems, int totalItems)> remainingWork)
        {
            foreach (var (team, remainingItems, totalItems) in remainingWork)
            {
                FeatureWork.Add(new FeatureWork(team, remainingItems, totalItems, this));
            }
        }

        public WhenForecast Forecast
        {
            get
            {
                return new AggregatedWhenForecast(Forecasts);
            }
        }

        public List<WhenForecast> Forecasts { get; set; } = [];

        // When the simulation expects work on this Feature to begin - one entry per contributing Team,
        // plus one for the Feature itself whose TeamId is null. Deliberately not in Forecasts: the
        // Forecast property above folds that collection into one distribution without asking what is in
        // it, so a start day in there would move every completion date in the product.
        public List<StartForecast> StartForecasts { get; set; } = [];

        public List<FeatureWork> FeatureWork { get; } = new List<FeatureWork>();

        public List<Portfolio> Portfolios { get; } = [];

        // Handed out read-only because the sync rewrites this list wholesale on every refresh. A caller
        // that added or removed a reference here would be writing half a graph that the next refresh
        // silently throws away, so only the reconciler that owns the rewrite may change a stored one.
        public IReadOnlyCollection<FeatureDependencyReference> DependsOnReferences => dependsOnReferences;

        public bool IsParentFeature { get; set; } = false;

        public bool IsUsingDefaultFeatureSize { get; set; } = false;

        public int Size
        {
            get
            {
                if (IsUsingDefaultFeatureSize)
                {
                    return 0;
                }

                return FeatureWork.Sum(fw => fw.TotalWorkItems);
            }
        }

        public int EstimatedSize { get; set; } = 0;

        // This instance's own place for the Feature, never the tracker's. Deliberately absent from
        // Update - the sync writes Order and nothing else, so a refresh cannot undo a manual move.
        public int? ManualRank { get; set; }

        public string OwningTeam { get; set; } = string.Empty;

        [NotMapped]
        public IEnumerable<Team> Teams => FeatureWork.Select(t => t.Team);

        [NotMapped]
        public bool CanBeForecast => !TeamsWithoutForecast.Any();

        /// <summary>
        /// When work on this Feature begins. The state settles whether work has begun; the dates only
        /// supply the value. A Feature being worked on, or already finished, has started - so it reports
        /// the day it started, or failing that the day it was created, and reports nothing only when it
        /// has neither. Never a prediction: a prediction of when work will start is false about work that
        /// has already begun.
        ///
        /// The creation day stands in because a start date is routinely missing through no fault of the
        /// data - a work tracking system with an empty started-date column maps every in-progress item to
        /// one - and answering nothing there takes the Feature off the timeline under the untrue heading
        /// that nothing can be forecast for it. It is also the stand-in this Feature's age and cycle time
        /// are already measured from, so the two cannot end up disagreeing about the same Feature.
        ///
        /// The simulation is never told what is in flight. This is decided on the way out, so nothing
        /// about how a forecast is produced depends on it.
        /// </summary>
        [NotMapped]
        public FeatureStart WhenWorkBegins
        {
            get
            {
                if (StateCategory is StateCategories.Doing or StateCategories.Done)
                {
                    return (StartedDate ?? CreatedDate) is { } begunOn ? FeatureStart.On(begunOn) : FeatureStart.NotKnown;
                }

                if (!CanBeForecast)
                {
                    return FeatureStart.NotKnown;
                }

                // The Feature's own row, the one no single team owns. Per-team rows name their team.
                var acrossEveryTeam = StartForecasts.Find(forecast => forecast.TeamId is null);

                return acrossEveryTeam is null || acrossEveryTeam.TotalTrials == 0
                    ? FeatureStart.NotKnown
                    : FeatureStart.ExpectedFrom(acrossEveryTeam);
            }
        }

        /// <summary>What the simulation expects of one contributing team, or nothing if it has no answer.</summary>
        public StartForecast? StartForecastFor(Team team)
            => SomethingToSay(StartForecasts.Find(forecast => forecast.TeamId == team.Id));

        /// <summary>What the simulation expects of one contributing team's completion, mirroring the above.</summary>
        public WhenForecast? CompletionForecastFor(Team team)
            => SomethingToSay(Forecasts.Find(forecast => (forecast.Team?.Id ?? forecast.TeamId) == team.Id));

        /// <summary>
        /// A distribution built from no runs at all is not an answer, and it is worse than none: every
        /// percentile off an empty histogram reads as day zero, which every date projection turns into
        /// today. A team that has never delivered would report that it finishes today, at every
        /// confidence level, in the same shape a real forecast arrives in.
        ///
        /// A team with nothing measured is exactly the case the rest of the product is careful to say
        /// nothing about - `TeamsWithoutForecast` names it, and `CanBeForecast` blanks the Feature's own
        /// dates because of it. The per-team rows have to answer the same way.
        /// </summary>
        private static TForecast? SomethingToSay<TForecast>(TForecast? forecast) where TForecast : ForecastBase
            => forecast is not null && forecast.TotalTrials > 0 ? forecast : null;

        // A team that must still finish but has no throughput leaves the feature with no honest
        // completion distribution. A feature with no remaining work is exempt - it carries
        // ForecastService's day-0 sentinel, which has no trials either, but is a fact, not a forecast.
        [NotMapped]
        public IEnumerable<Team> TeamsWithoutForecast
        {
            get
            {
                if (FeatureWork.Sum(work => work.RemainingWorkItems) <= 0)
                {
                    return [];
                }

                var withoutThroughput = Forecasts
                    .Where(forecast => forecast.TotalTrials == 0)
                    .Select(TeamFor);

                // A pair added by work-item sync after the last forecast run has no row at all, which
                // is strictly worse than a zero-trial one - nothing has been simulated for it.
                var withoutAnyRow = FeatureWork
                    .Where(work => work.RemainingWorkItems > 0)
                    .Where(work => !HasForecastRowFor(work))
                    .Select(work => work.Team);

                // Distinct at the source: AddOrUpdateWorkForTeam treats duplicate pairs for one team as
                // reachable, and both clauses can name the same team.
                return withoutThroughput
                    .Concat(withoutAnyRow)
                    .Where(team => team is not null)
                    .Select(team => team!)
                    .Distinct();
            }
        }

        private Team? TeamFor(WhenForecast forecast)
        {
            return forecast.Team ?? Teams.FirstOrDefault(team => team.Id == forecast.TeamId);
        }

        private bool HasForecastRowFor(FeatureWork work)
        {
            return Forecasts.Exists(forecast => (forecast.Team?.Id ?? forecast.TeamId) == work.TeamId);
        }

        public double? GetLikelhoodForDate(DateTime date, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            if (date != default && FeatureWork.Sum(r => r.RemainingWorkItems) > 0)
            {
                // An un-forecastable feature reports "unknown", not a number.
                if (!CanBeForecast)
                {
                    return null;
                }

                var timeToTargetDate = blackoutPeriods.CountWorkingDays(InstanceCalendar.AsUtcMidnight(today), date);

                return Forecast?.GetLikelihood(timeToTargetDate) ?? 0;
            }

            return 100;
        }

        public void AddOrUpdateWorkForTeam(Team team, int remainingWork, int totalItems)
        {
            var existingEntries = FeatureWork.Where(t => t.TeamId == team.Id).ToList();

            if (existingEntries.Count == 0)
            {
                var featureWork = new FeatureWork(team, remainingWork, totalItems, this);
                FeatureWork.Add(featureWork);
            }
            else
            {
                // Remove duplicates if any exist (data corruption recovery)
                for (var i = 1; i < existingEntries.Count; i++)
                {
                    FeatureWork.Remove(existingEntries[i]);
                }

                existingEntries[0].RemainingWorkItems = remainingWork;
                existingEntries[0].TotalWorkItems = totalItems;
            }
        }

        public void RemoveTeamFromFeature(Team team)
        {
            var existingEntries = FeatureWork.Where(t => t.TeamId == team.Id).ToList();
            foreach (var entry in existingEntries)
            {
                FeatureWork.Remove(entry);
            }
        }

        public int GetRemainingWorkForTeam(Team team)
        {
            var existingTeam = FeatureWork.FirstOrDefault(t => t.TeamId == team.Id);
            if (existingTeam != null)
            {
                return existingTeam.RemainingWorkItems;
            }

            return -1;
        }

        public void SetFeatureForecasts(IEnumerable<WhenForecast> forecasts)
        {
            Forecasts.Clear();

            foreach (var forecast in forecasts)
            {
                forecast.Feature = this;
                forecast.FeatureId = Id;
                Forecasts.Add(forecast);
            }
        }

        /// <summary>
        /// Mirrors <see cref="SetFeatureForecasts"/>, and is called in the same pass. A forecast is
        /// current state rather than history, so both collections are cleared and rewritten in full on
        /// every run; a refresh that wrote one without the other would leave a Feature saying it starts
        /// on a day that has nothing to do with the day it says it finishes.
        /// </summary>
        public void SetStartForecasts(IEnumerable<StartForecast> startForecasts)
        {
            StartForecasts.Clear();

            foreach (var startForecast in startForecasts)
            {
                startForecast.Feature = this;
                startForecast.FeatureId = Id;
                StartForecasts.Add(startForecast);
            }
        }

        public void ClearFeatureWork()
        {
            foreach (var featureWork in FeatureWork)
            {
                featureWork.Clear();
            }
        }

        internal void ReplaceDependsOnReferences(IEnumerable<FeatureDependencyReference> references)
        {
            dependsOnReferences.Clear();
            dependsOnReferences.AddRange(references);
        }

        internal void Update(Feature feature)
        {
            base.Update(feature);

            EstimatedSize = feature.EstimatedSize;
            OwningTeam = feature.OwningTeam;
        }
    }
}
