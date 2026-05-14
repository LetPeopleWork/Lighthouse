using System.Net;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // DISTILL scaffold — implementation deferred to DELIVER Slice 04.
    // Verifies US-04 AC #3's load-bearing invariant: in standalone (Tauri desktop)
    // mode, no /api/oauth/* route is registered. Re-layered here from Playwright
    // scenario #13 on 2026-05-14 because route-table absence is an HTTP-level
    // invariant — Playwright cannot uniquely express "route is not registered"
    // (404 from a missing route looks identical to 404 from a guard).
    public class OAuthStandaloneModeRouteRejectionIntegrationTest()
        : IntegrationTestBase(new StandaloneModeTestWebApplicationFactory())
    {
        [Test]
        [Ignore("DELIVER Slice 04 — wire StandaloneModeTestWebApplicationFactory (overrides Lighthouse:Mode=Standalone or equivalent runtime flag), then unignore.")]
        public async Task StandaloneMode_OAuthConnectEndpoint_Returns404()
        {
            var response = await Client.PostAsync("/api/oauth/jira.oauth/connect", content: null);

            // The standalone-mode WAF must not register the OAuthController.
            // Assertion semantics: 404 (Not Found) is the only correct status here.
            // 405 (Method Not Allowed) or 501 (Not Implemented) would indicate the
            // route IS registered but the handler is missing — a violation of US-04 AC #3.
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "OAuth routes must not be registered in standalone mode (US-04 AC #3).");
        }

        [Test]
        [Ignore("DELIVER Slice 04 — wire StandaloneModeTestWebApplicationFactory, then unignore.")]
        public async Task StandaloneMode_OAuthCallbackEndpoint_Returns404()
        {
            var response = await Client.GetAsync("/api/oauth/callback?provider=jira.oauth&code=x&state=y");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                "OAuth callback must not be registered in standalone mode (US-04 AC #3).");
        }
    }

    // Placeholder for the standalone-mode WAF variant. DELIVER Slice 04 implements this
    // by overriding ConfigureWebHost to set the runtime mode flag the production code
    // reads to skip OAuth controller registration.
    internal sealed class StandaloneModeTestWebApplicationFactory : TestWebApplicationFactory<Program>
    {
        // TODO(DELIVER Slice 04): override ConfigureWebHost to set
        //   builder.UseSetting("Lighthouse:Mode", "Standalone")
        // OR whatever runtime flag US-04's implementation defines for the standalone-mode
        // skip path on OAuth controller registration.
    }
}
