using System.Net.Http.Headers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth
{
    public class OAuthBearerAuthStrategy(IOAuthService oauthService) : IWorkTrackingAuthStrategy
    {
        public async Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            var accessToken = await oauthService.EnsureFreshTokenAsync(connection.Id, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
