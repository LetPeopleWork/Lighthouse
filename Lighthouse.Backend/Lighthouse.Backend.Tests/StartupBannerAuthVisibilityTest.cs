using Lighthouse.Backend.Startup;
using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests
{
    public class StartupBannerAuthVisibilityTest
    {
        [TestCase(true, "Enabled")]
        [TestCase(false, "Disabled")]
        public void BuildAuthPostureLines_RendersAuthenticationLineWithEnabledOrDisabled(bool enabled, string expectedValue)
        {
            var configuration = BuildConfiguration(authenticationEnabled: enabled);

            var lines = AuthPostureBanner.BuildAuthPostureLines(configuration);

            Assert.That(lines, Has.Some.Contains("Authentication").And.Contains(expectedValue));
        }

        [TestCase(true, "Enabled")]
        [TestCase(false, "Disabled")]
        public void BuildAuthPostureLines_RendersAuthorizationLineWithEnabledOrDisabled(bool enabled, string expectedValue)
        {
            var configuration = BuildConfiguration(authorizationEnabled: enabled);

            var lines = AuthPostureBanner.BuildAuthPostureLines(configuration);

            Assert.That(lines, Has.Some.Contains("Authorization").And.Contains(expectedValue));
        }

        [Test]
        public void BuildAuthPostureLines_AuthorizationEnabledWithSubjects_IncludesEmergencyAdminLineWithCommaJoinedSubjects()
        {
            var configuration = BuildConfiguration(
                authorizationEnabled: true,
                emergencyAdminSubjects: new[] { "alice@example.com", "bob@example.com" });

            var lines = AuthPostureBanner.BuildAuthPostureLines(configuration);

            Assert.That(lines, Has.Some.Contains("Emergency Admin").And.Contains("alice@example.com, bob@example.com"));
        }

        [Test]
        public void BuildAuthPostureLines_AuthorizationEnabledWithoutSubjects_OmitsEmergencyAdminLine()
        {
            var configuration = BuildConfiguration(authorizationEnabled: true);

            var lines = AuthPostureBanner.BuildAuthPostureLines(configuration);

            Assert.That(lines, Has.None.Contains("Emergency Admin"));
        }

        [Test]
        public void BuildAuthPostureLines_AuthorizationDisabledWithSubjects_OmitsEmergencyAdminLine()
        {
            var configuration = BuildConfiguration(
                authorizationEnabled: false,
                emergencyAdminSubjects: new[] { "alice@example.com" });

            var lines = AuthPostureBanner.BuildAuthPostureLines(configuration);

            Assert.That(lines, Has.None.Contains("Emergency Admin"));
        }

        private static IConfiguration BuildConfiguration(
            bool? authenticationEnabled = null,
            bool? authorizationEnabled = null,
            string[]? emergencyAdminSubjects = null)
        {
            var data = new Dictionary<string, string?>();

            if (authenticationEnabled.HasValue)
            {
                data["Authentication:Enabled"] = authenticationEnabled.Value ? "true" : "false";
            }

            if (authorizationEnabled.HasValue)
            {
                data["Authorization:Enabled"] = authorizationEnabled.Value ? "true" : "false";
            }

            if (emergencyAdminSubjects != null)
            {
                for (var i = 0; i < emergencyAdminSubjects.Length; i++)
                {
                    data[$"Authorization:EmergencySystemAdminSubjects:{i}"] = emergencyAdminSubjects[i];
                }
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();
        }
    }
}
