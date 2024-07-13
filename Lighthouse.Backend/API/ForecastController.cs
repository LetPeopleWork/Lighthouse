using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForecastController : ControllerBase
    {
        private readonly IMonteCarloService monteCarloService;
        private readonly IRepository<Team> teamRepository;

        public ForecastController(IMonteCarloService monteCarloService, IRepository<Team> teamRepository)
        {
            this.monteCarloService = monteCarloService;
            this.teamRepository = teamRepository;
        }

        [HttpPost("update/{id}")]
        public async Task<ActionResult> UpdateForecastForTeamAsync(int id)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            await monteCarloService.ForecastFeaturesForTeam(team);

            return Ok();
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

                manualForecast.WhenForecasts.AddRange(whenForecast.CreateForecastDtos([50, 70, 85, 95]));

                if (timeToTargetDate > 0)
                {
                    manualForecast.Likelihood = whenForecast.GetLikelihood(timeToTargetDate);
                }
            }

            if (timeToTargetDate > 0)
            {
                var howManyForecast = monteCarloService.HowMany(team.Throughput, timeToTargetDate);

                manualForecast.HowManyForecasts.AddRange(howManyForecast.CreateForecastDtos([50, 70, 85, 95]));
            }

            return Ok(manualForecast);
        }

        public class ManualForecastInputDto
        {
            public int RemainingItems { get; set; }

            public DateTime TargetDate { get; set; }
        }
    }
}
