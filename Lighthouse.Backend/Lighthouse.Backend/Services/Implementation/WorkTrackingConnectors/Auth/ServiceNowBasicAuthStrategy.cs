using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    // SCAFFOLD (DISTILL slice 01, Story #5574) — ApplyAsync deliberately leaves the request
    // unauthenticated so ServiceNowBasicAuthStrategyTest fails at its assertions
    // (MISSING_FUNCTIONALITY). DELIVER writes the Basic header.
    //
    // Its own class rather than a reuse of JiraCloudBasicAuthStrategy: that one reads Jira option
    // keys by name and falls through to Bearer for the token methods, so reusing it would put
    // ServiceNow knowledge inside a Jira-named class.
    public class ServiceNowBasicAuthStrategy(ICryptoService cryptoService) : IWorkTrackingAuthStrategy
    {
        private readonly ICryptoService cryptoService = cryptoService;

        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(connection);

            _ = cryptoService;

            return Task.CompletedTask;
        }
    }
}
