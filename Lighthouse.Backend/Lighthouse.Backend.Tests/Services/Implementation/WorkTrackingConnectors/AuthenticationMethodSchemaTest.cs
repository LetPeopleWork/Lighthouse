using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
{
    public class AuthenticationMethodSchemaTest
    {
        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps, 1)]
        [TestCase(WorkTrackingSystems.Jira, 2)]
        [TestCase(WorkTrackingSystems.Linear, 1)]
        [TestCase(WorkTrackingSystems.Csv, 1)]
        public void GetMethodsForSystem_ReturnsExpectedNumberOfMethods(WorkTrackingSystems system, int expectedCount)
        {
            var methods = AuthenticationMethodSchema.GetMethodsForSystem(system);

            Assert.That(methods, Has.Count.EqualTo(expectedCount));
        }

        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps, AuthenticationMethodKeys.AzureDevOpsPat, "Personal Access Token")]
        [TestCase(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraCloud, "Jira Cloud (API Token)")]
        [TestCase(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraDataCenter, "Jira Data Center (Personal Access Token)")]
        [TestCase(WorkTrackingSystems.Linear, AuthenticationMethodKeys.LinearApiKey, "API Key")]
        [TestCase(WorkTrackingSystems.Csv, AuthenticationMethodKeys.None, "No Authentication")]
        public void GetDisplayName_ReturnsCorrectDisplayName(WorkTrackingSystems system, string key, string expectedDisplayName)
        {
            var displayName = AuthenticationMethodSchema.GetDisplayName(system, key);

            Assert.That(displayName, Is.EqualTo(expectedDisplayName));
        }

        [Test]
        public void GetMethodByKey_JiraCloud_HasUsernameOption()
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraCloud);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.Options.Any(o => o.Key == "Username"), Is.True);
        }

        [Test]
        public void GetMethodByKey_JiraDataCenter_DoesNotHaveUsernameOption()
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraDataCenter);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.Options.Any(o => o.Key == "Username"), Is.False);
        }

        [Test]
        public void GetMethodByKey_AllMethods_HaveNonEmptyDisplayName()
        {
            foreach (var system in Enum.GetValues<WorkTrackingSystems>())
            {
                var methods = AuthenticationMethodSchema.GetMethodsForSystem(system);

                foreach (var method in methods)
                {
                    Assert.That(method.DisplayName, Is.Not.Empty, 
                        $"Method {method.Key} for {system} should have a display name");
                }
            }
        }

        [Test]
        public void GetMethodByKey_SecretOptions_AreMarkedAsSecret()
        {
            var adoMethod = AuthenticationMethodSchema.GetMethodByKey(
                WorkTrackingSystems.AzureDevOps, 
                AuthenticationMethodKeys.AzureDevOpsPat);

            var patOption = adoMethod!.Options.Single(o => o.Key == "Personal Access Token");

            Assert.That(patOption.IsSecret, Is.True);
        }
    }
}
