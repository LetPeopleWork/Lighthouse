using System.Security.Claims;
using System.Text.Encodings.Web;
using Lighthouse.Backend.Modules.Access.Adapters.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.Access.Adapters.Http
{
    [TestFixture]
    public class DisabledAuthenticationHandlerTest
    {
        [Test]
        public async Task Every_request_is_signed_in_as_the_fixed_disabled_user()
        {
            var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
            options.Setup(o => o.Get(DisabledAuthenticationHandler.SchemeName)).Returns(new AuthenticationSchemeOptions());
            var handler = new DisabledAuthenticationHandler(options.Object, NullLoggerFactory.Instance, UrlEncoder.Default);
            await handler.InitializeAsync(
                new AuthenticationScheme(DisabledAuthenticationHandler.SchemeName, null, typeof(DisabledAuthenticationHandler)),
                new DefaultHttpContext());

            var result = await handler.AuthenticateAsync();

            var principal = result.Principal!;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(principal.FindFirst("sub")?.Value, Is.EqualTo("lighthouse|auth-disabled"));
                Assert.That(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, Is.EqualTo("lighthouse|auth-disabled"));
                Assert.That(principal.FindFirst("name")?.Value, Is.EqualTo("Authentication Disabled"));
                Assert.That(principal.Identity?.AuthenticationType, Is.EqualTo(DisabledAuthenticationHandler.SchemeName));
            }
        }
    }
}
