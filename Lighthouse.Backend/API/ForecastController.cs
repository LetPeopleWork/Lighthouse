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

        [HttpPost("{id}")]
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
    }
}
