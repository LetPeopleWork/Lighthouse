using CMFTAspNet.Models.Forecast;
using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Models
{
    public class Feature
    {
        public Feature() : this(Enumerable.Empty<(Team team, int remainingItems)>())
        {            
        }

        public Feature(Team team, int remainingItems) : this([(team, remainingItems)])
        {
        }

        public Feature(IEnumerable<(Team team, int remainingItems)> remainingWork)
        {
            RemainingWork = new Dictionary<Team, int>();
            foreach (var (team, remainingItems) in remainingWork)
            {
                RemainingWork.Add(team, remainingItems);
            }
        }

        public int Id { get; set; }

        public string Name { get; set; }
        
        public int Order { get; set; }

        public WhenForecast Forecast { get; private set; }

        public Dictionary<Team, int> RemainingWork { get; }

        public void SetFeatureForecast(IEnumerable<WhenForecast> forecasts)
        {
            var worstCaseForecast = 0;

            foreach (var forecast in forecasts)
            {
                var result = forecast.GetProbability(85);

                if (result > worstCaseForecast)
                {
                    worstCaseForecast = result;
                    Forecast = forecast;
                }
            }
        }
    }
}
