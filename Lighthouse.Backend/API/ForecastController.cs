using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForecastController : ControllerBase
    {
        private readonly IMonteCarloService monteCarloService;
        private readonly IRepository<Team> teamRepository;
        private readonly IRepository<Project> projectRepository;

        public ForecastController(IMonteCarloService monteCarloService, IRepository<Team> teamRepository, IRepository<Project> projectRepository)
        {
            this.monteCarloService = monteCarloService;
            this.teamRepository = teamRepository;
            this.projectRepository = projectRepository;
        }

        [HttpPost("update/{id}")]
        public async Task<ActionResult<ProjectDto>> UpdateForecastForProject(int id)
        {
            var project = projectRepository.GetById(id);

            if (project == null)
            {
                return NotFound();
            }

            await monteCarloService.UpdateForecastsForProject(project);

            return Ok(new ProjectDto(project));
        }

        [HttpPost("manual/{id}")]
        public async Task<ActionResult<ManualForecastDto>> RunManualForecastAsync(int id, [FromBody] ManualForecastInputDto input)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            var manualForecast = new ManualForecastDto(input.RemainingItems, input.TargetDate);

            var timeToTargetDate = (input.TargetDate - DateTime.Today).Days;

            if (input.RemainingItems > 0)
            {
                var whenForecast = await monteCarloService.When(team, input.RemainingItems);

                manualForecast.WhenForecasts.AddRange(whenForecast.CreateForecastDtos(50, 70, 85, 95));

                if (timeToTargetDate > 0)
                {
                    manualForecast.Likelihood = whenForecast.GetLikelihood(timeToTargetDate);
                }
            }

            if (timeToTargetDate > 0)
            {
                var howManyForecast = monteCarloService.HowMany(team.Throughput, timeToTargetDate);

                manualForecast.HowManyForecasts.AddRange(howManyForecast.CreateForecastDtos(50, 70, 85, 95));
            }

            return Ok(manualForecast);
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
