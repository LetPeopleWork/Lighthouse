using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

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
                new NoOpAuthStrategy(),
                new OAuthBearerAuthStrategy(Mock.Of<IOAuthService>(), NullLogger<OAuthBearerAuthStrategy>.Instance));
        }
    }
}
