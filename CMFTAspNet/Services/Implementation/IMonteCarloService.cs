using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Services.Implementation
{
    public interface IMonteCarloService
    {
        void ForecastFeatures(IEnumerable<Feature> features);

        HowManyForecast HowMany(Throughput throughput, int days);

        WhenForecast When(Team team, int remainingItems);
    }
}