using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Services.Implementation
{
    public interface IMonteCarloService
    {
        Task ForecastAllFeatures();

        Task ForecastFeaturesForTeam(Team team);

        HowManyForecast HowMany(Throughput throughput, int days);

        Task<WhenForecast> When(Team team, int remainingItems);
    }
}