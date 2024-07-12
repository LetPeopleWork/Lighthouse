using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThroughputController : ControllerBase
    {
        private IThroughputService throughputService;
        private IRepository<Team> teamRepository;

        public ThroughputController(IThroughputService throughputService, IRepository<Team> teamRepository)
        {
            this.throughputService = throughputService;
            this.teamRepository = teamRepository;
        }

        [HttpPost("{id}")]
        public async Task<ActionResult> UpdateThroughput(int id)
        {
            var team = teamRepository.GetById(id);

            if (team == null)
            {
                return NotFound();
            }

            await throughputService.UpdateThroughputForTeam(team);

            await teamRepository.Save();

            return Ok();
        }
    }
}
