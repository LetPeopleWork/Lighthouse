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
        public ActionResult<Throughput> GetThroughput(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, teamMetricsService.GetThroughputForTeam);
        }

        [HttpGet("featuresInProgress")]
        public ActionResult<List<string>> GetFeaturesInProgress(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, teamMetricsService.GetFeaturesInProgressForTeam);
        }
    }
}