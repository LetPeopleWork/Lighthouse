using System.Net.Http.Headers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class OAuthBearerAuthStrategy(IOAuthService oauthService, ILogger<OAuthBearerAuthStrategy> logger) : IWorkTrackingAuthStrategy
    {
        public async Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            var accessToken = await oauthService.EnsureFreshTokenAsync(connection.Id, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            logger.LogDebug("OAuth Bearer token applied to outbound request for connection {ConnectionId}.", connection.Id);
        }
    }
}
