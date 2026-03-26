using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpGet("login")]
        public IActionResult Login()
        {
            var status = authModeResolver.Resolve();

            if (status.Mode is not (AuthMode.Enabled or AuthMode.Blocked))
            {
                return NotFound();
            }

            return Challenge(
                new AuthenticationProperties { RedirectUri = "/" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var status = authModeResolver.Resolve();

            if (status.Mode is not (AuthMode.Enabled or AuthMode.Blocked))
            {
                return NotFound();
            }

            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet("session")]
        [ProducesResponseType<AuthSessionStatus>(StatusCodes.Status200OK)]
        public IActionResult GetSession()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            var sessionStatus = new AuthSessionStatus
            {
                IsAuthenticated = isAuthenticated,
                DisplayName = isAuthenticated
                    ? User.FindFirst("name")?.Value ?? User.Identity?.Name
                    : null,
                Email = isAuthenticated
                    ? User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value
                    : null,
            };

            return Ok(sessionStatus);
        }
    }
}
