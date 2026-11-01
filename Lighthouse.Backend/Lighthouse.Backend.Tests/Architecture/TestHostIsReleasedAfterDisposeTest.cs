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
    /// limiters' refill timers) are what point at a host; this fixture starts a host, serves one request,
    /// disposes it, and checks that it can really be collected.
    ///
    /// It runs alone: another fixture's allocations would slow the collection polling, and a neighbour's
    /// host cannot be mistaken for the one under test.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class TestHostIsReleasedAfterDisposeTest
    {
        private static readonly TimeSpan HowLongToWaitForTheHostToBeFreed = TimeSpan.FromSeconds(15);

        private const string ARouteThatDoesNotExist = "/api/latest/zz-nothing-here";

        [OneTimeSetUp]
        public void StartAndDropAWarmUpHost()
        {
            // The first host of every process stays reachable through one-shot timers that other
            // components create once; it is not the leak, so it is built and dropped before measuring.
            _ = StartServeAndDisposeAHost(resolveEveryOAuthProvider: false);
        }

        [Test]
        [Ignore("Enabled when the test-host leak fix lands")]
        public void AHostThatServedARequestIsFreedOnceDisposed()
        {
            var host = StartServeAndDisposeAHost(resolveEveryOAuthProvider: false);

            Assert.That(
                WaitUntilFreed(host),
                Is.True,
                "A disposed test host is still reachable after " + HowLongToWaitForTheHostToBeFreed.TotalSeconds +
                " seconds of collections. Something outside the host (a timer, a static) still points at it.");
        }

        [Test]
        [Ignore("Enabled when the test-host leak fix lands")]
        public void AHostThatResolvedEveryOAuthProviderIsFreedOnceDisposed()
        {
            var host = StartServeAndDisposeAHost(resolveEveryOAuthProvider: true);

            Assert.That(
                WaitUntilFreed(host),
                Is.True,
                "A disposed test host that resolved the OAuth providers is still reachable after " +
                HowLongToWaitForTheHostToBeFreed.TotalSeconds + " seconds of collections. A provider is probably " +
                "holding an HTTP client instead of asking the client factory for one per request.");
        }

        // Synchronous and never inlined, so no local of the calling test (or of an async state machine)
        // can keep the host alive; only the weak reference leaves this method.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference StartServeAndDisposeAHost(bool resolveEveryOAuthProvider)
        {
            var factory = new TestWebApplicationFactory<Program>();

            using (var client = factory.CreateClient())
            {
                using var response = client.GetAsync(ARouteThatDoesNotExist).GetAwaiter().GetResult();
            }

            if (resolveEveryOAuthProvider)
            {
                _ = factory.Services.GetServices<IOAuthProvider>().ToList();
            }

            var weakReference = new WeakReference(factory);
            factory.Dispose();
            return weakReference;
        }

        private static bool WaitUntilFreed(WeakReference host)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < HowLongToWaitForTheHostToBeFreed)
            {
#pragma warning disable S1215 // The test asks "can this object be collected?", which only a forced full collection can answer.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
#pragma warning restore S1215

                if (!host.IsAlive)
                {
                    return true;
                }

                Thread.Sleep(250);
            }

            return !host.IsAlive;
        }
    }
}
