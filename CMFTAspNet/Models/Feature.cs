using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Models
{
    public class Feature
    {
        public Feature(Throughput throughput, int remainingItems)
        {
            Throughput = throughput;
            RemainingItems = remainingItems;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public Throughput Throughput { get; }

        public int RemainingItems { get; }

        public WhenForecast Forecast { get; private set; }

        public void SetFeatureForecast(WhenForecast forecast)
        {
            Forecast = forecast;
        }

        public Feature Copy()
        {
            return new Feature(Throughput, RemainingItems);
        }
    }
}
