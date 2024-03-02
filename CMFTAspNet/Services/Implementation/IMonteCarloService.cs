using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;

namespace CMFTAspNet.Services.Implementation
{
    public interface IMonteCarloService
    {
        Task ForecastAllFeatures();

        Task ForecastFeaturesForTeam(Team team);

        HowManyForecast HowMany(Throughput throughput, int days);        

        WhenForecast When(Team team, int remainingItems);
    }
}