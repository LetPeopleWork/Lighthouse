using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;

namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    public interface IForecastService
    {
        Task UpdateForecastsForProject(Portfolio portfolio);

        HowManyForecast HowMany(RunChartData throughput, int days);

        HowManyForecast PredictWorkItemCreation(Team team, string[] workItemTypes, DateTime startDate, DateTime endDate, int daysToForecast);


        Task<WhenForecast> When(Team team, int remainingItems);    }
}