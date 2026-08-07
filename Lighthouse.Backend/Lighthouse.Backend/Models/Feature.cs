﻿using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lighthouse.Backend.Models
{
    public class Feature : WorkItemBase
    {
        public Feature() : this([])
        {
        }

        public Feature(WorkItemBase workItemBase) : base(workItemBase)
        {
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

        public List<FeatureWork> FeatureWork { get; } = new List<FeatureWork>();

        public List<Portfolio> Portfolios { get; } = [];

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

        // This instance's own place for the Feature, never the tracker's (ADR-132). Deliberately absent
        // from Update - the sync writes Order and nothing else (ADR-134 SA-4).
        public int? ManualRank { get; set; }

        public string OwningTeam { get; set; } = string.Empty;

        [NotMapped]
        public IEnumerable<Team> Teams => FeatureWork.Select(t => t.Team);

        [NotMapped]
        public bool CanBeForecast => !TeamsWithoutForecast.Any();

        // A team that must still finish but has no throughput leaves the feature with no honest
        // completion distribution (ADR-112). A feature with no remaining work is exempt - it carries
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
                // is strictly worse than a zero-trial one (ADR-113 DDD-8).
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
                // An un-forecastable feature reports "unknown", not a number (ADR-112).
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

        public void ClearFeatureWork()
        {
            foreach (var featureWork in FeatureWork)
            {
                featureWork.Clear();
            }
        }

        internal void Update(Feature feature)
        {
            base.Update(feature);

            EstimatedSize = feature.EstimatedSize;
            OwningTeam = feature.OwningTeam;
        }
    }
}
