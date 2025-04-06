using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Metrics;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/teams/{teamId}/metrics")]
    [ApiController]
    public class TeamMetricsController : ControllerBase
    {
        private readonly IRepository<Team> teamRepository;
        private readonly ITeamMetricsService teamMetricsService;

        public TeamMetricsController(IRepository<Team> teamRepository, ITeamMetricsService teamMetricsService)
        {
            this.teamRepository = teamRepository;
            this.teamMetricsService = teamMetricsService;
        }

        [HttpGet("throughput")]
        public ActionResult<Throughput> GetThroughput(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetThroughputForTeam(team, startDate, endDate));
        }

        [HttpGet("featuresInProgress")]
        public ActionResult<List<WorkItemDto>> GetFeaturesInProgress(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, teamMetricsService.GetCurrentFeaturesInProgressForTeam);
        }

        [HttpGet("wip")]
        public ActionResult<List<WorkItemDto>> GetCurrentWipForTeam(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, teamMetricsService.GetCurrentWipForTeam);
        }
    }
}