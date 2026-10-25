using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// The Features list of a Portfolio as the Features tab draws it: every row, the parents it hangs under and
    /// which Teams are at work, in one answer.
    /// </summary>
    [Route("api/v1/portfolios/{portfolioId:int}/feature-list")]
    [Route("api/latest/portfolios/{portfolioId:int}/feature-list")]
    [ApiController]
    public class FeatureListController(IFeatureListService featureListService, FeatureListRows rows) : ControllerBase
    {
        [HttpGet]
        [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
        public async Task<ActionResult<FeatureListDto>> ForPortfolio(int portfolioId)
        {
            var read = await featureListService.ForPortfolioAsync(User, portfolioId, HttpContext.RequestAborted);

            return Ok(rows.Of(read));
        }
    }
}
