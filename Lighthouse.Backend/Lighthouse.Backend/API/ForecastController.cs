﻿using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForecastController : ControllerBase
    {
        private readonly IForecastUpdateService forecastUpdateService;
        private readonly IRepository<Team> teamRepository;
        private readonly ITeamMetricsService teamMetricsService;

        public ForecastController(IForecastUpdateService forecastUpdateService, IRepository<Team> teamRepository, ITeamMetricsService teamMetricsService)
        {
            this.forecastUpdateService = forecastUpdateService;
            this.teamRepository = teamRepository;
            this.teamMetricsService = teamMetricsService;
        }

        [HttpPost("update/{id}")]
        public ActionResult UpdateForecastForProject(int id)
        {
            forecastUpdateService.TriggerUpdate(id);

            return Ok();
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
                    var whenForecast = await forecastUpdateService.When(team, input.RemainingItems);

                    manualForecast.WhenForecasts.AddRange(whenForecast.CreateForecastDtos(50, 70, 85, 95));

                    if (timeToTargetDate > 0)
                    {
                        manualForecast.Likelihood = whenForecast.GetLikelihood(timeToTargetDate);
                    }
                }

                if (timeToTargetDate > 0)
                {
                    var throughput = teamMetricsService.GetThroughputForTeam(team);
                    var howManyForecast = forecastUpdateService.HowMany(throughput, timeToTargetDate);

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
    }
}
