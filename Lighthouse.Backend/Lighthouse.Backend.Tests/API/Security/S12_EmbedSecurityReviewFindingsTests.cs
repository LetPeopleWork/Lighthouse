using System.Text.Json;
using Lighthouse.Backend.Configuration;

namespace Lighthouse.Backend.Tests.API.Security
{
    // Epic 5146 slice 02a (#5641) — the S1-S10 security review of 2026-08-04, finding F1.
    // F2 and F3 guarded the API-key embed session, which no longer exists; F3's control now reads
    // "deleting the viewer ends their live frame" and lives in S14.
    public class S12_EmbedSecurityReviewFindingsTests
    {
        // F1. Every other rate-limit test configures the policy it then exercises, so all of them pass
        // whether or not the shipped configuration defines one. An undefined policy does not fail
        // loudly: Program.cs falls through to GetNoLimiter, and the endpoint runs unthrottled forever.
        // This reads the file that ships, which is the only thing that can catch that.
        [Test]
        public void EveryDeclaredRateLimitPolicy_IsDefinedInTheShippedConfiguration()
        {
            var appsettingsPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory, "appsettings.json");

            Assert.That(File.Exists(appsettingsPath), Is.True,
                $"appsettings.json is expected beside the test assembly, at {appsettingsPath}");

            using var document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
            var policies = document.RootElement
                .GetProperty(RateLimitingConfiguration.SectionName)
                .GetProperty("Policies");

            string[] declaredPolicies =
            [
                RateLimitingConfiguration.AuthLoginPolicy,
                RateLimitingConfiguration.ApiKeysPolicy,
                RateLimitingConfiguration.BootstrapSystemAdminPolicy,
                RateLimitingConfiguration.EmbedSessionPolicy,
            ];

            using (Assert.EnterMultipleScope())
            {
                foreach (var policy in declaredPolicies)
                {
                    Assert.That(policies.TryGetProperty(policy, out _), Is.True,
                        $"policy '{policy}' is referenced by an endpoint but undefined in appsettings.json, "
                        + "so the limiter silently permits everything");
                }
            }
        }
    }
}
