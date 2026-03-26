using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthModeResolver authModeResolver;

        public AuthController(IAuthModeResolver authModeResolver)
        {
            this.authModeResolver = authModeResolver;
        }

        [HttpGet("mode")]
        [ProducesResponseType<RuntimeAuthStatus>(StatusCodes.Status200OK)]
        public IActionResult GetRuntimeAuthStatus()
        {
            var status = authModeResolver.Resolve();
            return Ok(status);
        }
    }
}
