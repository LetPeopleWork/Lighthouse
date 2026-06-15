using System.Collections.Generic;
using System.Linq;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    [Ignore("pending: backend-test-speed (ADO #5258) Slice-04 — un-skip after the integration + " +
            "service/mock fixtures are isolated (ADR-074) and the allowlist below is final. RED today: " +
            "~54 off-allowlist [NonParallelizable] tags remain. DISTILL scaffold per ADR-025.")]
    public class BackendTestParallelizationGuardTest
    {
        // Fixtures that are INHERENTLY serial — process-global state or deliberate concurrency —
        // and therefore legitimately keep [NonParallelizable]. Every OTHER opt-out must be removed
        // by fixing its shared-state root cause (per-fixture WAF/DB or per-test mocks). Slice-04
        // finalises this set against docs/feature/backend-test-speed/triage.md.
        private static readonly IReadOnlySet<string> AllowedSerialFixtures = new HashSet<string>
        {
            "S6_RateLimitingTests",
            "S1_AllowedOriginsEnvVarBindingTests",
            "S1_CorsFailClosedTests",
            "S5_ApiKeyScopesTests",
            "F_BE_1_GroupSnapshotInheritanceTests",
            "LighthouseAppContextConcurrencyTest",
        };

        [Test]
        public void NoFixtureOptsOutOfParallelizationWithoutBeingOnTheAllowlist()
        {
            var offenders = typeof(BackendTestParallelizationGuardTest).Assembly
                .GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(NonParallelizableAttribute), inherit: false).Length > 0)
                .Select(type => type.Name)
                .Where(name => !AllowedSerialFixtures.Contains(name))
                .OrderBy(name => name)
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "backend-test-speed (#5258): a test fixture carries [NonParallelizable] but is not on the " +
                "inherently-serial allowlist. Fix its shared-state root cause (per-fixture WebApplicationFactory/DB " +
                "or per-test mocks, ADR-074) and remove the attribute. Add to the allowlist ONLY if the test is " +
                "genuinely serial (process-global state / deliberate concurrency) — with a reason, per docs/ci-learnings.md. " +
                "Off-allowlist offenders: " + string.Join(", ", offenders));
        }
    }
}
