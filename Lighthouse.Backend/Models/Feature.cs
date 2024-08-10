using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Feature : IEntity
    {
        public Feature() : this(Enumerable.Empty<(Team team, int remainingItems, int totalItems)>())
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

        public int Id { get; set; }

        public string ReferenceId { get; set; } = string.Empty;

        public string Name { get; set; }

        public string Order { get; set; }

        public string? Url { get; set; }

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

        public double GetLikelhoodForDate(DateTime date)
        {
            if (date != default && FeatureWork.Sum(r => r.RemainingWorkItems) > 0)
            {
                var timeToTargetDate = (date - DateTime.Today).Days;

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
    }
}
