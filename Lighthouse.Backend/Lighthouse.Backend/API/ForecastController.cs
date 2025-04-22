using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
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
    public class ForecastController : ControllerBase
    {
        private readonly IForecastUpdater forecastUpdater;
        private readonly IForecastService forecastService;
        private readonly IRepository<Team> teamRepository;
        private readonly ITeamMetricsService teamMetricsService;

        public ForecastController(IForecastUpdater forecastUpdater, IForecastService forecastService, IRepository<Team> teamRepository, ITeamMetricsService teamMetricsService)
        {
            this.forecastUpdater = forecastUpdater;
            this.forecastService = forecastService;
            this.teamRepository = teamRepository;
            this.teamMetricsService = teamMetricsService;
        }

        [HttpPost("update/{id}")]
        public ActionResult UpdateForecastForProject(int id)
        {
            forecastUpdater.TriggerUpdate(id);

            return Ok();
        }

        [HttpPost("itemprediction/{id}")]
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

        [HttpPost("manual/{id}")]
        public async Task<ActionResult<ManualForecastDto>> RunManualForecastAsync(int id, [FromBody] ManualForecastInputDto input)
        {
            return await this.GetEntityByIdAnExecuteAction(teamRepository, id, async team =>
            {
                var manualForecast = new ManualForecastDto(input.RemainingItems, input.TargetDate);

                var timeToTargetDate = (input.TargetDate - DateTime.Today).Days;

                if (input.RemainingItems > 0)
                {
                    var whenForecast = await forecastService.When(team, input.RemainingItems);

                    manualForecast.WhenForecasts.AddRange(whenForecast.CreateForecastDtos(50, 70, 85, 95));

                    if (timeToTargetDate > 0)
                    {
                        manualForecast.Likelihood = whenForecast.GetLikelihood(timeToTargetDate);
                    }
                }

                if (timeToTargetDate > 0)
                {
                    var throughput = teamMetricsService.GetCurrentThroughputForTeam(team);
                    var howManyForecast = forecastService.HowMany(throughput, timeToTargetDate);

                    manualForecast.HowManyForecasts.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));
                }

                return manualForecast;
            });
        }

        public class ManualForecastInputDto
        {
            [JsonRequired]
            public int RemainingItems { get; set; }

            [JsonRequired]
            public DateTime TargetDate { get; set; }
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
