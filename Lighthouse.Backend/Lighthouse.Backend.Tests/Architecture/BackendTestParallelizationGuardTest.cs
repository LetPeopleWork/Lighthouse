using System.Collections.Generic;
using System.Linq;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class BackendTestParallelizationGuardTest
    {
        private static readonly IReadOnlyDictionary<string, string> AllowedSerialFixtures = new Dictionary<string, string>
        {
            ["S6_RateLimitingTests"] = "per-IP rate-limiter window/partition is process-wide; parallel requests cross-trip the limiter",
            ["S1_AllowedOriginsEnvVarBindingTests"] = "sets Authentication__AllowedOrigins* process-global environment variables in setup/teardown",
            ["S1_CorsFailClosedTests"] = "sets CORS/auth process-global environment variables in setup/teardown",
            ["LighthouseReleaseServiceIntegrationTest"] = "shares a static IGitHubService as a GitHub rate-limit workaround across tests",
        };

        [Test]
        public void NoFixtureOptsOutOfParallelizationWithoutBeingOnTheAllowlist()
        {
            var offenders = TaggedFixtureNames()
                .Where(name => !AllowedSerialFixtures.ContainsKey(name))
                .OrderBy(name => name)
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "backend-test-speed (#5258): a test fixture carries [NonParallelizable] but is not on the inherently-serial " +
                "allowlist. Fix its shared-state root cause (per-fixture WebApplicationFactory/DB or per-test mocks, ADR-074) and " +
                "remove the attribute. Add it to the allowlist ONLY if the test is genuinely serial (process-global state / " +
                "deliberate concurrency) — with a reason, per docs/ci-learnings.md. Off-allowlist offenders: " + string.Join(", ", offenders));
        }

        [Test]
        public void EveryAllowlistedFixtureStillOptsOutOfParallelization()
        {
            var taggedFixtures = TaggedFixtureNames().ToHashSet();

            var staleEntries = AllowedSerialFixtures.Keys
                .Where(name => !taggedFixtures.Contains(name))
                .OrderBy(name => name)
                .ToList();

            Assert.That(
                staleEntries,
                Is.Empty,
                "backend-test-speed (#5258): an allowlist entry no longer carries [NonParallelizable] — its shared-state root " +
                "cause was fixed. Remove it from the allowlist so the serial residue stays honest (see docs/ci-learnings.md). " +
                "Stale entries: " + string.Join(", ", staleEntries));
        }

        private static IEnumerable<string> TaggedFixtureNames()
        {
            return typeof(BackendTestParallelizationGuardTest).Assembly
                .GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(NonParallelizableAttribute), inherit: false).Length > 0)
                .Select(type => type.Name);
        }
    }
}
