using Lighthouse.Models;
using Lighthouse.Models.Forecast;

namespace Lighthouse.Services.Implementation
{
    public interface IMonteCarloService
    {
        Task ForecastAllFeatures();

        Task ForecastFeaturesForTeam(Team team);

        HowManyForecast HowMany(Throughput throughput, int days);        

        WhenForecast When(Team team, int remainingItems);
    }
}