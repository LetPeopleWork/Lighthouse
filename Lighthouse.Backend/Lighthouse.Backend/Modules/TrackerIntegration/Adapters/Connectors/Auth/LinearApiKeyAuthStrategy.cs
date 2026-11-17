using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Linear;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth
{
    public class LinearApiKeyAuthStrategy(ICryptoService cryptoService) : IWorkTrackingAuthStrategy
    {
        private const string AuthorizationHeaderName = "Authorization";

        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            var encryptedApiKey = connection.GetWorkTrackingSystemConnectionOptionByKey(LinearWorkTrackingOptionNames.ApiKey);
            var apiKey = cryptoService.Decrypt(encryptedApiKey);

            request.Headers.Remove(AuthorizationHeaderName);
            request.Headers.Add(AuthorizationHeaderName, apiKey);

            return Task.CompletedTask;
        }
    }
}
