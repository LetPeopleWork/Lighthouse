using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The backend suite starts about two thousand application hosts in one process. A host that something
    /// outside it still points at after it is disposed is never freed, and the memory of the whole run climbs
    /// with every such host. Timers kept alive by the runtime (the HTTP client cleanup timer and the rate
    /// limiters' refill timers) are what point at a host. Test hosts therefore run without rate limiting
    /// unless a fixture tests it, and a host with it on is expected to stay reachable. This fixture starts a
    /// default host, serves one request, disposes it, and checks that it can really be collected.
    ///
    /// It runs alone: another fixture's allocations would slow the collection polling and add noise to the
    /// measurement.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class TestHostIsReleasedAfterDisposeTest : IntegrationFixture
    {
        private static readonly TimeSpan HowLongToWaitForAHostThatMadeNoClient = TimeSpan.FromSeconds(15);

        // A host that created one factory client is held until the test host's one-second handler lifetime
        // has passed and the factory's own cleanup tick (every 10 seconds) has run, so it needs a longer wait.
        private static readonly TimeSpan HowLongToWaitForAHostThatMadeOneClient = TimeSpan.FromSeconds(30);

        private const string ARouteThatDoesNotExist = "/api/latest/zz-nothing-here";

        private enum WhatTheHostDoes
        {
            ServesOneRequest,
            ResolvesEveryOAuthProvider,
            CreatesOneFactoryClient,
        }

        [OneTimeSetUp]
        public void StartAndDropAWarmUpHost()
        {
            // Builds one host first so the one-shot process-wide timers, which pin the first host a process creates, do not land on a measured host.
            _ = StartAndDisposeAHost(WhatTheHostDoes.ServesOneRequest);
        }

        [Test]
        public void AHostThatServedARequestIsFreedOnceDisposed()
        {
            var host = StartAndDisposeAHost(WhatTheHostDoes.ServesOneRequest);

            var outcome = WaitUntilFreed(host, HowLongToWaitForAHostThatMadeNoClient);

            Assert.That(
                outcome.Freed,
                Is.True,
                "A disposed test host is still reachable after " + Describe(outcome) +
                " Something outside the host (a timer, a static) still points at it. " +
                "Default test hosts run without rate limiting; check that the default is still off.");
        }

        [Test]
        public void AHostThatResolvedEveryOAuthProviderIsFreedOnceDisposed()
        {
            var host = StartAndDisposeAHost(WhatTheHostDoes.ResolvesEveryOAuthProvider);

            var outcome = WaitUntilFreed(host, HowLongToWaitForAHostThatMadeNoClient);

            Assert.That(
                outcome.Freed,
                Is.True,
                "A disposed test host that resolved the OAuth providers is still reachable after " + Describe(outcome) +
                " A provider is probably holding an HTTP client instead of asking the client factory for one per request.");
        }

        [Test]
        public void AHostThatCreatedOneHttpClientIsFreedOnceDisposed()
        {
            var host = StartAndDisposeAHost(WhatTheHostDoes.CreatesOneFactoryClient);

            var outcome = WaitUntilFreed(host, HowLongToWaitForAHostThatMadeOneClient);

            Assert.That(
                outcome.Freed,
                Is.True,
                "A disposed test host that created one HTTP client is still reachable after " + Describe(outcome) +
                " The test host should give HTTP client handlers a lifetime of one second; check that it does.");
        }

        private static string Describe((bool Freed, TimeSpan Elapsed, int Collections) outcome) =>
            outcome.Elapsed.TotalSeconds.ToString("F1") + " seconds (" + outcome.Collections + " rounds of collections).";

        // Synchronous and never inlined, so no local of the calling test (or of an async state machine)
        // can keep the host alive; only the weak reference leaves this method.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference StartAndDisposeAHost(WhatTheHostDoes whatTheHostDoes)
        {
            var factory = new TestWebApplicationFactory<Program>();

            using (var client = factory.CreateClient())
            {
                using var response = client.GetAsync(ARouteThatDoesNotExist).GetAwaiter().GetResult();
            }

            if (whatTheHostDoes == WhatTheHostDoes.ResolvesEveryOAuthProvider)
            {
                _ = factory.Services.GetServices<IOAuthProvider>().ToList();
            }

            if (whatTheHostDoes == WhatTheHostDoes.CreatesOneFactoryClient)
            {
                _ = factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient(JiraOAuthProvider.HttpClientName);
            }

            var weakReference = new WeakReference(factory);
            factory.Dispose();
            return weakReference;
        }

        private static (bool Freed, TimeSpan Elapsed, int Collections) WaitUntilFreed(WeakReference host, TimeSpan limit)
        {
            var stopwatch = Stopwatch.StartNew();
            var collections = 0;

            while (stopwatch.Elapsed < limit)
            {
#pragma warning disable S1215 // The test asks "can this object be collected?", which only a forced full collection can answer.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
#pragma warning restore S1215
                collections++;

                if (!host.IsAlive)
                {
                    return (true, stopwatch.Elapsed, collections);
                }

                Thread.Sleep(250);
            }

            return (false, stopwatch.Elapsed, collections);
        }
    }
}
