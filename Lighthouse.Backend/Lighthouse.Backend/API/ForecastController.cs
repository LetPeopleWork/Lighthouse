using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForecastController(
        IForecastUpdater forecastUpdater,
        IForecastService forecastService,
        IRepository<Team> teamRepository,
        ITeamMetricsService teamMetricsService)
        : ControllerBase
    {
        [HttpPost("update/{id:int}")]
        public ActionResult UpdateForecastForProject(int id)
        {
            forecastUpdater.TriggerUpdate(id);

            return Ok();
        }

        [HttpPost("update-portfolios-for-team/{teamId:int}")]
        public ActionResult<bool> UpdateForecastsForTeamPortfolios(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                foreach (var portfolio in team.Portfolios)
                {
                    forecastUpdater.TriggerUpdate(portfolio.Id);
                }

                return true;
            });
        }

        [HttpPost("itemprediction/{id:int}")]
        [LicenseGuard(RequirePremium = true)]
        public ActionResult<ManualForecastDto> RunItemCreationPrediction(int id, [FromBody] ItemCreationPredictionInputDto input)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, id, team =>
            {
                var itemCreationPrediction = new ManualForecastDto(0, input.TargetDate);
                var timeToTargetDate = (input.TargetDate - DateTime.Today).Days;

                var howManyForecast = forecastService.PredictWorkItemCreation(team, input.WorkItemTypes, input.StartDate, input.EndDate, timeToTargetDate);
                itemCreationPrediction.HowManyForecasts.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));

                return itemCreationPrediction;
            });
        }

        [HttpPost("manual/{id:int}")]
        public async Task<ActionResult<ManualForecastDto>> RunManualForecastAsync(int id, [FromBody] ManualForecastInputDto input)
        {
            return await this.GetEntityByIdAnExecuteAction(teamRepository, id, async team =>
            {
                var manualForecast = new ManualForecastDto(input.RemainingItems, input.TargetDate);

                var timeToTargetDate = input.TargetDate.HasValue ? (input.TargetDate.Value - DateTime.Today).Days : 0;

                if (input.RemainingItems > 0)
                {
                    var whenForecast = await forecastService.When(team, input.RemainingItems);

                    manualForecast.WhenForecasts.AddRange(whenForecast.CreateForecastDtos(50, 70, 85, 95));

                    if (timeToTargetDate > 0)
                    {
                        manualForecast.Likelihood = whenForecast.GetLikelihood(timeToTargetDate);
                    }
                }

                if (timeToTargetDate <= 0)
                {
                    return manualForecast;
                }

                var throughput = teamMetricsService.GetCurrentThroughputForTeam(team);
                var howManyForecast = forecastService.HowMany(throughput, timeToTargetDate);

                manualForecast.HowManyForecasts.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));

                return manualForecast;
            });
        }

        [HttpPost("backtest/{teamId:int}")]
        public ActionResult<BacktestResultDto> RunBacktest(int teamId, [FromBody] BacktestInputDto input)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var minStartDate = today.AddDays(-14);
            if (input.StartDate > minStartDate)
            {
                return BadRequest("StartDate must be at least 14 days in the past.");
            }

            var forecastDays = input.EndDate.DayNumber - input.StartDate.DayNumber;
            if (forecastDays < 14)
            {
                return BadRequest("EndDate must be at least 14 days after StartDate.");
            }

            if (input.HistoricalWindowDays <= 0)
            {
                return BadRequest("HistoricalWindowDays must be a positive number.");
            }

            if (input.HistoricalWindowDays > 365)
            {
                return BadRequest("HistoricalWindowDays must not exceed 365.");
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                var historyStart = input.StartDate.AddDays(-input.HistoricalWindowDays).ToDateTime(TimeOnly.MinValue);
                var historyEnd = input.StartDate.ToDateTime(TimeOnly.MinValue);
                var historicalThroughput = teamMetricsService.GetThroughputForTeam(team, historyStart, historyEnd);

                var howManyForecast = forecastService.HowMany(historicalThroughput, forecastDays);

                var periodStart = input.StartDate.ToDateTime(TimeOnly.MinValue);
                var periodEnd = input.EndDate.ToDateTime(TimeOnly.MinValue);
                var actualThroughput = teamMetricsService.GetThroughputForTeam(team, periodStart, periodEnd);

                var result = new BacktestResultDto(input.StartDate, input.EndDate, input.HistoricalWindowDays)
                {
                    ActualThroughput = actualThroughput.Total
                };

                result.Percentiles.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));

                return result;
            });
        }

        public class ManualForecastInputDto
        {
            public int RemainingItems { get; set; }

            public DateTime? TargetDate { get; set; }
        }

        public class ItemCreationPredictionInputDto
        {
            [JsonRequired]
            public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);

            [JsonRequired]
            public DateTime EndDate { get; set; } = DateTime.Today;

            [JsonRequired]
            public DateTime TargetDate { get; set; } = DateTime.Today.AddDays(30);

            [JsonRequired]
            public string[] WorkItemTypes { get; set; } = [];
        }
    }
}
