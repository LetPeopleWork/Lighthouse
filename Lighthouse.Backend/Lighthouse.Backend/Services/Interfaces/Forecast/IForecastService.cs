using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    public interface IForecastService
    {
        Task UpdateForecastsForProject(Project project);

        HowManyForecast HowMany(RunChartData throughput, int days);

        Task<WhenForecast> When(Team team, int remainingItems);
    }
}