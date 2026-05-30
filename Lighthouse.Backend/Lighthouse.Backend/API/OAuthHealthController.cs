using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/oauth/health")]
    [ApiController]
    [Authorize]
    [RbacGuard(RbacGuardRequirement.SystemAdmin)]
    public sealed class OAuthHealthController : ControllerBase
    {
        private readonly IOAuthHealthAggregator aggregator;

        public OAuthHealthController(IOAuthHealthAggregator aggregator)
        {
            this.aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        }

        [HttpGet]
        public async Task<ActionResult<OAuthHealthDto>> GetHealth(CancellationToken cancellationToken)
        {
            var dto = await aggregator.AggregateAsync(cancellationToken);
            return Ok(dto);
        }
    }
}
