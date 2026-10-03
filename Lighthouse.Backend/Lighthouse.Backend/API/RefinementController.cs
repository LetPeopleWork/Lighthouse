using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/teams/{teamId:int}/refinement")]
    [Route("api/latest/teams/{teamId:int}/refinement")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class RefinementController(IRefinementViewQuery refinementViewQuery, ILighthouseClock clock) : ControllerBase
    {
        [HttpGet]
        public ActionResult<RefinementViewDto> GetRefinement(int teamId)
        {
            var view = refinementViewQuery.ForTeam(teamId);
            if (view is null)
            {
                return NotFound();
            }

            return Ok(new RefinementViewDto(view, clock));
        }
    }
}
