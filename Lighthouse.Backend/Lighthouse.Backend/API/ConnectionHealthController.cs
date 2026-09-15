using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.ConnectionHealth;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.ConnectionHealth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// Connection health names every connection on the instance and says what is wrong with it, which
    /// is the same instance-wide operational detail the refresh log and the log file already decided
    /// is administrator-only.
    /// </summary>
    [Route("api/latest/connectionhealth")]
    [ApiController]
    [Authorize]
    [RbacGuard(RbacGuardRequirement.SystemAdmin)]
    public sealed class ConnectionHealthController : ControllerBase
    {
        private readonly IConnectionHealthService connectionHealthService;

        public ConnectionHealthController(IConnectionHealthService connectionHealthService)
        {
            this.connectionHealthService = connectionHealthService ?? throw new ArgumentNullException(nameof(connectionHealthService));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConnectionHealthDto>>> GetHealth()
        {
            return Ok(await connectionHealthService.GetHealthAsync());
        }

        [HttpPost("{connectionId}/test")]
        public async Task<ActionResult<ConnectionHealthDto>> TestConnection(int connectionId)
        {
            var verdict = await connectionHealthService.TestConnectionAsync(connectionId);

            return verdict == null ? NotFound() : Ok(verdict);
        }
    }
}
