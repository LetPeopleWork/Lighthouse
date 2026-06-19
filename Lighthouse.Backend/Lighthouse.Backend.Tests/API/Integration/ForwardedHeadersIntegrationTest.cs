using System.Net;
using Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [Category("epic-5305-k8s-readiness")]
    public class ForwardedHeadersIntegrationTest
    {
        private const string TrustedProxyIp = "10.20.30.40";
        private const string LoopbackSource = "::1";
        private const string PublicHost = "lighthouse.public.example";

        [Test]
        public async Task ForwardedHeaders_TrustedProxyHttpsProto_RequestSchemeBecomesHttps()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.TrustingProxy(TrustedProxyIp, simulatedRemoteIp: TrustedProxyIp));

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: PublicHost);

            Assert.That(observed.Scheme, Is.EqualTo("https"));
        }

        [Test]
        public async Task ForwardedHeaders_TrustedProxyForwardedHost_RequestHostBecomesPublicHost()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.TrustingProxy(TrustedProxyIp, simulatedRemoteIp: TrustedProxyIp));

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: PublicHost);

            Assert.That(observed.Host, Is.EqualTo(PublicHost));
        }

        [Test]
        public async Task ForwardedHeaders_TrustedProxyAndHttps_OidcChallengeRedirectUriIsHttpsPublicHost()
        {
            using var host = new ForwardedHeadersOidcTestHost(TrustedProxyIp, IPAddress.Parse(TrustedProxyIp));

            var challenge = await host.ChallengeLoginAsync(forwardedProto: "https", forwardedHost: PublicHost);

            var redirectUri = Uri.UnescapeDataString(challenge.Location);
            Assert.That(redirectUri, Does.Contain($"redirect_uri=https://{PublicHost}/api/auth/callback"));
        }

        [Test]
        public async Task ForwardedHeaders_TrustedProxyAndHttps_AuthCookieIsSecure()
        {
            using var host = new ForwardedHeadersOidcTestHost(TrustedProxyIp, IPAddress.Parse(TrustedProxyIp));

            var challenge = await host.ChallengeLoginAsync(forwardedProto: "https", forwardedHost: PublicHost);

            Assert.That(challenge.SetCookies, Is.Not.Empty);
            Assert.That(challenge.SetCookies, Has.All.Contain("secure"));
        }

        [Test]
        public async Task ForwardedHeaders_TrustedNetworkCidr_RequestSchemeBecomesHttps()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.TrustingNetwork("10.20.30.0/24", simulatedRemoteIp: "10.20.30.40"));

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: PublicHost);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observed.Scheme, Is.EqualTo("https"));
                Assert.That(observed.Host, Is.EqualTo(PublicHost));
            }
        }

        [Test]
        public async Task ForwardedHeaders_SourceOutsideTrustedNetwork_SchemeNotRewritten()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.TrustingNetwork("10.20.30.0/24", simulatedRemoteIp: "10.99.99.99"));

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: PublicHost);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observed.Scheme, Is.EqualTo("http"));
                Assert.That(observed.Host, Is.Not.EqualTo(PublicHost));
            }
        }

        [Test]
        public async Task ForwardedHeaders_UndeclaredSourceProto_SchemeNotRewritten()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.UntrustedSource(TrustedProxyIp, simulatedRemoteIp: LoopbackSource));

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: PublicHost);

            Assert.That(observed.Scheme, Is.EqualTo("http"));
        }

        [Test]
        public async Task ForwardedHeaders_UndeclaredSourceHost_HostNotSpoofed()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.UntrustedSource(TrustedProxyIp, simulatedRemoteIp: LoopbackSource));

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: "attacker.example");

            Assert.That(observed.Host, Is.Not.EqualTo("attacker.example"));
        }

        [Test]
        public async Task ForwardedHeaders_NoProxyDeclared_DirectAccessByteIdentical()
        {
            using var host = new ForwardedHeadersTestHost(ForwardedHeadersTestOptions.Standalone());

            var observed = await host.GetObservedRequestAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observed.Scheme, Is.EqualTo("http"));
                Assert.That(observed.Host, Is.EqualTo("localhost"));
            }
        }

        [Test]
        public async Task ForwardedHeaders_TrustOffByDefault_ForwardedHeadersIgnored()
        {
            using var host = new ForwardedHeadersTestHost(
                ForwardedHeadersTestOptions.Standalone() with { SimulatedRemoteIp = IPAddress.Parse(LoopbackSource) });

            var observed = await host.GetObservedRequestAsync(forwardedProto: "https", forwardedHost: "attacker.example");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observed.Scheme, Is.EqualTo("http"));
                Assert.That(observed.Host, Is.Not.EqualTo("attacker.example"));
            }
        }
    }
}
