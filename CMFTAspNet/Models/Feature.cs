using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Models
{
    public class Feature
    {
        public Feature(Team team, int remainingItems)
        {
            Team = team;
            RemainingItems = remainingItems;
        }

        public Guid Id { get; } = Guid.NewGuid();
        
        public Team Team { get; }

        public int RemainingItems { get; }

        public WhenForecast Forecast { get; private set; }

        public void SetFeatureForecast(WhenForecast forecast)
        {
            Forecast = forecast;
        }
    }
}
