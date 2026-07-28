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

                return Forecasts
                    .Where(forecast => forecast.TotalTrials == 0)
                    .Select(TeamFor)
                    .Where(team => team is not null)
                    .Select(team => team!);
            }
        }

        private Team? TeamFor(WhenForecast forecast)
        {
            return forecast.Team ?? Teams.FirstOrDefault(team => team.Id == forecast.TeamId);
        }

        public double? GetLikelhoodForDate(DateTime date, DateOnly today, IReadOnlyList<BlackoutPeriod> blackoutPeriods)
        {
            if (date != default && FeatureWork.Sum(r => r.RemainingWorkItems) > 0)
            {
                // Unknown is carried out explicitly. Falling through would hit ForecastBase.GetLikelihood's
                // trialCounter == 0 branch and report 100 % on the one feature nobody can forecast (ADR-112).
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
