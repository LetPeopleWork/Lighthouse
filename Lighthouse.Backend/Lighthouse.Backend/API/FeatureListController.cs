using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// The Features list of a Portfolio or a Team as the Features tab draws it: every row, the parents it hangs under and
    /// which Teams are at work, in one answer. An owner that does not exist is not found; one the reader may not open is refused.
    /// </summary>
    [Route("api/v1")]
    [Route("api/latest")]
    [ApiController]
    public class FeatureListController(IFeatureListService featureListService, FeatureListRows rows) : ControllerBase
    {
        [HttpGet("portfolios/{portfolioId:int}/feature-list")]
        public async Task<ActionResult<FeatureListDto>> ForPortfolio(int portfolioId)
        {
            var access = await featureListService.AccessToPortfolioAsync(User, portfolioId, HttpContext.RequestAborted);
            if (access != FeatureListAccess.Granted)
            {
                return Refusal(access);
            }

            var read = await featureListService.ForPortfolioAsync(User, portfolioId, HttpContext.RequestAborted);

            return Ok(rows.Of(read));
        }

        [HttpGet("teams/{teamId:int}/feature-list")]
        public async Task<ActionResult<FeatureListDto>> ForTeam(int teamId)
        {
            var access = await featureListService.AccessToTeamAsync(User, teamId, HttpContext.RequestAborted);
            if (access != FeatureListAccess.Granted)
            {
                return Refusal(access);
            }

            var read = await featureListService.ForTeamAsync(User, teamId, HttpContext.RequestAborted);

            return Ok(rows.Of(read));
        }

        private ActionResult Refusal(FeatureListAccess access)
            => access == FeatureListAccess.OwnerMissing ? NotFound() : Forbid();
    }
}
