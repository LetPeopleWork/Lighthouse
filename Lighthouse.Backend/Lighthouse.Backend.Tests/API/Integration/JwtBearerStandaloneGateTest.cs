using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class JwtBearerStandaloneGateTest : IntegrationTestBase
    {
        [Test]
        public async Task AuthDisabled_NoJwtBearerSchemeRegistered()
        {
            var schemeProvider = ServiceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

            var jwtScheme = await schemeProvider.GetSchemeAsync(SmartAuthSchemeSelector.JwtBearerScheme);

            Assert.That(jwtScheme, Is.Null);
        }
    }
}
