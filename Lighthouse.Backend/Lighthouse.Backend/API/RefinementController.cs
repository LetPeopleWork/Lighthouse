using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/teams/{teamId:int}/refinement")]
    [Route("api/latest/teams/{teamId:int}/refinement")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class RefinementController(IRefinementViewQuery refinementViewQuery) : ControllerBase
    {
        public const string VoterKeyHeader = "X-Lighthouse-Voter-Key";

        [HttpGet]
        public ActionResult<RefinementViewDto> GetRefinement(int teamId, [FromHeader(Name = VoterKeyHeader)] string? voterKey)
        {
            var view = refinementViewQuery.ForTeam(teamId, voterKey);
            if (view is null)
            {
                return NotFound();
            }

            return Ok(new RefinementViewDto(view));
        }
    }
}
