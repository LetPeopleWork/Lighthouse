﻿using Lighthouse.Backend.Models.Forecast;
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

        public List<Project> Projects { get; } = [];

        public bool IsUnparentedFeature { get; set; }

        public bool IsParentFeature { get; set; } = false;

        public bool IsUsingDefaultFeatureSize { get; set; } = false;

        public int EstimatedSize { get; set; } = 0;

        public string OwningTeam { get;set; } = string.Empty;

        public override bool IsBlocked => Projects.Any(p => p.BlockedStates.Contains(State) || p.BlockedTags.Any(Tags.Contains));

        [NotMapped]
        public IEnumerable<Team> Teams => FeatureWork.Select(t => t.Team);

        public double GetLikelhoodForDate(DateTime date)
        {
            if (date != default && FeatureWork.Sum(r => r.RemainingWorkItems) > 0)
            {
                var timeToTargetDate = (date - DateTime.UtcNow.Date).Days;

                return Forecast?.GetLikelihood(timeToTargetDate) ?? 0;
            }

            return 100;
        }

        public void AddOrUpdateWorkForTeam(Team team, int remainingWork, int totalItems)
        {
            var existingTeam = FeatureWork.SingleOrDefault(t => t.Team == team);
            if (existingTeam == null)
            {
                var featureWork = new FeatureWork(team, remainingWork, totalItems, this);
                FeatureWork.Add(featureWork);
            }
            else
            {
                existingTeam.RemainingWorkItems = remainingWork;
                existingTeam.TotalWorkItems = totalItems;
            }
        }

        public void RemoveTeamFromFeature(Team team)
        {
            var existingTeam = FeatureWork.SingleOrDefault(t => t.Team == team);
            if (existingTeam != null)
            {
                FeatureWork.Remove(existingTeam);
            }
        }

        public int GetRemainingWorkForTeam(Team team)
        {
            var existingTeam = FeatureWork.SingleOrDefault(t => t.Team == team);
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
                Forecast.Feature = this;
                Forecast.FeatureId = Id;
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
