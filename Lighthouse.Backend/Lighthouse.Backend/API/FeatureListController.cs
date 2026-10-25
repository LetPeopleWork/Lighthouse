using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// The Features list of a Portfolio or a Team as the Features tab draws it: every row, the parents it hangs under and
    /// which Teams are at work, in one answer. An owner that does not exist, or that the reader may not open, is not found.
    /// </summary>
    [Route("api/v1")]
    [Route("api/latest")]
    [ApiController]
    public class FeatureListController(IFeatureListService featureListService, FeatureListRows rows) : ControllerBase
    {
        [HttpGet("portfolios/{portfolioId:int}/feature-list")]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        public async Task<ActionResult<FeatureListDto>> ForPortfolio(int portfolioId)
        {
            if (!featureListService.PortfolioExists(portfolioId))
            {
                return NotFound();
            }

            var read = await featureListService.ForPortfolioAsync(User, portfolioId, HttpContext.RequestAborted);

            return Ok(rows.Of(read));
        }

        [HttpGet("teams/{teamId:int}/feature-list")]
        [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
        public async Task<ActionResult<FeatureListDto>> ForTeam(int teamId)
        {
            if (!featureListService.TeamExists(teamId))
            {
                return NotFound();
            }

            var read = await featureListService.ForTeamAsync(User, teamId, HttpContext.RequestAborted);

            return Ok(rows.Of(read));
        }
    }
}
