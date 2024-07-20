using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Feature : IEntity
    {
        public Feature() : this(Enumerable.Empty<(Team team, int remainingItems)>())
        {
        }

        public Feature(Team team, int remainingItems) : this([(team, remainingItems)])
        {
        }

        public Feature(IEnumerable<(Team team, int remainingItems)> remainingWork)
        {
            foreach (var (team, remainingItems) in remainingWork)
            {
                RemainingWork.Add(new RemainingWork(team, remainingItems, this));
            }
        }

        public int Id { get; set; }

        public string ReferenceId { get; set; } = string.Empty;

        public string Name { get; set; }

        public string Order { get; set; }

        public WhenForecast Forecast { get; set; }

        public List<RemainingWork> RemainingWork { get; } = new List<RemainingWork>();

        public List<Project> Projects { get; } = [];

        public bool IsUnparentedFeature { get; set; }

        public double GetLikelhoodForDate(DateTime date)
        {
            if (date != default && RemainingWork.Sum(r => r.RemainingWorkItems) > 0)
            {
                var timeToTargetDate = (date - DateTime.Today).Days;

                return Forecast?.GetLikelihood(timeToTargetDate) ?? 0;
            }

            return 100;
        }

        public void AddOrUpdateRemainingWorkForTeam(Team team, int remainingWork)
        {
            var existingTeam = RemainingWork.SingleOrDefault(t => t.Team == team);
            if (existingTeam == null)
            {
                var newRemainingWork = new RemainingWork(team, remainingWork, this);
                RemainingWork.Add(newRemainingWork);
            }
            else
            {
                existingTeam.RemainingWorkItems = remainingWork;
            }
        }

        public void RemoveTeamFromFeature(Team team)
        {
            var existingTeam = RemainingWork.SingleOrDefault(t => t.Team == team);
            if (existingTeam != null)
            {
                RemainingWork.Remove(existingTeam);
            }
        }

        public int GetRemainingWorkForTeam(Team team)
        {
            var existingTeam = RemainingWork.SingleOrDefault(t => t.Team == team);
            if (existingTeam != null)
            {
                return existingTeam.RemainingWorkItems;
            }

            return -1;
        }

        public void SetFeatureForecast(IEnumerable<WhenForecast> forecasts)
        {
            var worstCaseForecast = int.MinValue;

            foreach (var forecast in forecasts)
            {
                var result = forecast.GetProbability(85);

                if (result > worstCaseForecast)
                {
                    worstCaseForecast = result;
                    Forecast = forecast;
                    Forecast.Feature = this;
                    Forecast.FeatureId = Id;
                }
            }
        }
    }
}
