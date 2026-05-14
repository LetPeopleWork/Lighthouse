namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth
{
    // DISTILL scaffold — implementation deferred to DELIVER Slice 02.
    // Verifies the single-flight invariant on IOAuthService.EnsureFreshTokenAsync:
    // N concurrent callers observing an expired credential MUST trigger exactly one
    // refresh against the IOAuthProvider; all N callers receive the same fresh token.
    // Re-layered here from Playwright scenario #8 on 2026-05-14 because concurrency
    // under load is best asserted via direct service-level testing, not through an
    // E2E browser harness (where deterministically simulating concurrency is brittle).
    [TestFixture]
    public class OAuthRefreshSingleFlightTest
    {
        [Test]
        [Ignore("DELIVER Slice 02 — wire OAuthService.EnsureFreshTokenAsync + ConcurrentDictionary<int, SemaphoreSlim>, then unignore. See ADR-010-oauth-single-flight-refresh.md.")]
        public Task EnsureFreshTokenAsync_NConcurrentCallers_RefreshInvokedExactlyOnce()
        {
            // Arrange (DELIVER Slice 02 implementation):
            //   1. Build the OAuthService with a CountingStubOAuthProvider whose
            //      RefreshTokenAsync increments an interlocked counter and returns
            //      a deterministic new token after a small artificial delay (e.g. 50ms)
            //      to maximise the chance of overlap.
            //   2. Seed an OAuthCredential with Status=Valid and ExpiresAt 3 minutes
            //      from now (within the 5-minute refresh threshold per ADR-010).
            //
            // Act:
            //   const int CONCURRENT_CALLERS = 32;
            //   var tasks = Enumerable.Range(0, CONCURRENT_CALLERS)
            //       .Select(_ => oauthService.EnsureFreshTokenAsync(credential.Id, CancellationToken.None))
            //       .ToArray();
            //   var tokens = await Task.WhenAll(tasks);
            //
            // Assert:
            //   - stubProvider.RefreshInvocationCount == 1 (single-flight invariant).
            //   - tokens.Distinct().Count() == 1 (all callers got the same token).
            //   - DatabaseContext.OAuthCredentials.SingleAsync(c => c.Id == credential.Id)
            //     has the new AccessToken / RefreshToken / ExpiresAt persisted exactly once.
            //   - (Optional) Verify the structured log event "oauth.token.refreshed" was
            //     emitted exactly once, not N times.
            Assert.Fail("RED scaffold — implementation pending in DELIVER Slice 02.");
            return Task.CompletedTask;
        }
    }
}
