using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthModeResolver authModeResolver;

        public AuthController(IAuthModeResolver authModeResolver)
        {
            this.authModeResolver = authModeResolver;
        }

        [HttpGet("mode")]
        [AllowAnonymous]
        [ProducesResponseType<RuntimeAuthStatus>(StatusCodes.Status200OK)]
        public IActionResult GetRuntimeAuthStatus()
        {
            var status = authModeResolver.Resolve();
            return Ok(status);
        }

        [HttpGet("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingConfiguration.AuthLoginPolicy)]
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
        [AllowAnonymous]
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
        [AllowAnonymous]
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

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType<CurrentUserProfileStatus>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCurrentUserProfile(
            [FromServices] ICurrentUserProfileService currentUserProfileService,
            CancellationToken cancellationToken)
        {
            var userProfile = await currentUserProfileService.GetOrCreateFromPrincipalAsync(User, cancellationToken);

            if (userProfile is null)
            {
                return Forbid();
            }

            return Ok(new CurrentUserProfileStatus
            {
                Subject = userProfile.Subject,
                DisplayName = userProfile.DisplayName,
                Email = userProfile.Email,
            });
        }
    }
}
