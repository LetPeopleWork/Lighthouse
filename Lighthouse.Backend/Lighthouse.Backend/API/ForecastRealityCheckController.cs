using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// Shares the forecast routes with ForecastController but stands on its own, so the check takes only the
    /// three collaborators it reads through and none of the forecast controller's writers.
    /// </summary>
    [Route("api/v1/forecast")]
    [Route("api/latest/forecast")]
    [ApiController]
    public class ForecastRealityCheckController(
        IForecastRealityCheckService realityCheckService,
        IRepository<Team> teamRepository,
        ITeamMetricsService teamMetricsService)
        : ControllerBase
    {
        [HttpPost("reality-check/{teamId:int}")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public ActionResult<RealityCheckResultDto> RunRealityCheck(int teamId, [FromBody] RealityCheckInputDto input)
        {
            return this.GetEntityByIdAnExecuteAction(teamRepository, teamId, team =>
            {
                var mode = ThroughputFilterOverride.ToFilterMode(input.ApplyFilterOverride);
                var result = realityCheckService.Run(team, mode);
                var status = teamMetricsService.GetForecastThroughputStatus(team, mode);

                return result with { FilterApplied = status.FilterApplied, ExcludedSummary = status.ExcludedSummary };
            });
        }
    }
}
