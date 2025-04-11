using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IForecastUpdateService : IUpdateService
    {
        Task UpdateForecastsForProject(int projectId);

        HowManyForecast HowMany(RunChartData throughput, int days);

        Task<WhenForecast> When(Team team, int remainingItems);
    }
}