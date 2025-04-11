using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
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
        public ActionResult<RunChartData> GetThroughput(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetThroughputForTeam(team, startDate, endDate));
        }

        [HttpGet("featuresInProgress")]
        public ActionResult<IEnumerable<FeatureDto>> GetFeaturesInProgress(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var features = teamMetricsService.GetCurrentFeaturesInProgressForTeam(team);

                return features.Select(f => new FeatureDto(f));
            });
        }

        [HttpGet("wip")]
        public ActionResult<IEnumerable<WorkItemDto>> GetCurrentWipForTeam(int teamId)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var workItems = teamMetricsService.GetCurrentWipForTeam(team);
                return workItems.Select(w => new WorkItemDto(w));
            });
        }

        [HttpGet("cycleTimePercentiles")]
        public ActionResult<IEnumerable<PercentileValue>> GetCycleTimePercentilesForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) => teamMetricsService.GetCycleTimePercentilesForTeam(team, startDate, endDate));
        }

        [HttpGet("cycleTimeData")]
        public ActionResult<IEnumerable<WorkItemDto>> GetCycleTimeDataForTeam(int teamId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Start date must be before end date.");
            }

            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, (team) =>
            {
                var workItems = teamMetricsService.GetClosedItemsForTeam(team, startDate, endDate);
                return workItems.Select(w => new WorkItemDto(w));
            });
        }
    }
}