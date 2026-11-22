using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Moq;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    internal static class TestAuthStrategyFactory
    {
        public static IWorkTrackingAuthStrategyFactory CreateRealFactory(ICryptoService cryptoService)
        {
            return new WorkTrackingAuthStrategyFactory(
                new PatAuthStrategy(cryptoService),
                new JiraCloudBasicAuthStrategy(cryptoService),
                new LinearApiKeyAuthStrategy(cryptoService),
                new ServiceNowBasicAuthStrategy(cryptoService),
                new NoOpAuthStrategy(),
                new OAuthBearerAuthStrategy(Mock.Of<IOAuthService>()));
        }
    }
}
