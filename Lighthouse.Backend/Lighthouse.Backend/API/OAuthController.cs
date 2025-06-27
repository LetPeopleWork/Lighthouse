using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuthService oAuthService;
        private readonly ILogger<OAuthController> logger;

        public OAuthController(IOAuthService oAuthService, ILogger<OAuthController> logger)
        {
            this.oAuthService = oAuthService;
            this.logger = logger;
        }        [HttpGet("authorize")]        public IActionResult GetAuthorizationUrl([FromQuery] string state, [FromQuery] string clientId, [FromQuery] string redirectUri)
        {
            try
            {
                logger.LogInformation("Generating OAuth authorization URL with state: {State}", state);
                
                var authUrl = oAuthService.GetJiraAuthorizationUrl(clientId, redirectUri, state);
                
                return Ok(new { authorizationUrl = authUrl });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating OAuth authorization URL");
                return StatusCode(500, "Failed to generate authorization URL");
            }
        }

        [HttpPost("token")]
        public async Task<IActionResult> ExchangeCodeForToken([FromBody] TokenExchangeRequest request)
        {
            try
            {
                logger.LogInformation("Exchanging OAuth code for token");
                
                var tokenResponse = await oAuthService.ExchangeCodeForTokens(request.ClientId, request.ClientSecret, request.Code, request.RedirectUri);
                
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exchanging OAuth code for token");
                return StatusCode(500, "Failed to exchange code for token");
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                logger.LogInformation("Refreshing OAuth token");
                
                var tokenResponse = await oAuthService.RefreshAccessToken(request.ClientId, request.ClientSecret, request.RefreshToken);
                
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing OAuth token");
                return StatusCode(500, "Failed to refresh token");
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            try
            {
                logger.LogInformation("Validating OAuth token");
                
                var isValid = await oAuthService.ValidateAccessToken(request.AccessToken, request.JiraUrl);
                
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating OAuth token");
                return StatusCode(500, "Failed to validate token");            }
        }

        [HttpPost("accessible-resources")]
        public async Task<IActionResult> GetAccessibleResources([FromBody] AccessibleResourcesRequest request)
        {
            try
            {
                logger.LogInformation("Getting accessible resources");
                
                var resources = await oAuthService.GetAccessibleResources(request.AccessToken);
                
                return Ok(resources ?? new List<AccessibleResource>());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting accessible resources");
                return StatusCode(500, "Failed to get accessible resources");
            }
        }
    }public class TokenExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }    public class ValidateTokenRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string JiraUrl { get; set; } = string.Empty;
    }

    public class AccessibleResourcesRequest
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
