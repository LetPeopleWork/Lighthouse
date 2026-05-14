using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
{
    public class AuthenticationMethodSchemaTest
    {
        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps, 1)]
        [TestCase(WorkTrackingSystems.Jira, 4)]
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
        [TestCase(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraScopedToken, "Jira Cloud (Scoped Access Token)")]
        [TestCase(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraOAuth, "Jira Cloud (OAuth)")]
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
        public void GetMethodByKey_JiraScopedToken_HasUrlOption()
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraScopedToken);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.Options.Any(o => o.Key == "Jira Url"), Is.True);
        }

        [Test]
        public void GetMethodByKey_JiraScopedToken_HasUsernameOption()
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraScopedToken);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.Options.Any(o => o.Key == "Username"), Is.True);
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

        [Test]
        public void AuthenticationMethod_IsPremium_DefaultsToFalse()
        {
            var method = new AuthenticationMethod
            {
                Key = "some.key",
                DisplayName = "Some Method",
                Options = []
            };

            Assert.That(method.IsPremium, Is.False);
        }

        [Test]
        [TestCase(AuthenticationMethodKeys.JiraCloud, false)]
        [TestCase(AuthenticationMethodKeys.JiraDataCenter, false)]
        [TestCase(AuthenticationMethodKeys.JiraScopedToken, false)]
        [TestCase(AuthenticationMethodKeys.JiraOAuth, true)]
        public void GetMethodByKey_Jira_HasExpectedIsPremiumFlag(string methodKey, bool expectedIsPremium)
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(WorkTrackingSystems.Jira, methodKey);

            Assert.That(method, Is.Not.Null);
            Assert.That(method!.IsPremium, Is.EqualTo(expectedIsPremium));
        }

        [Test]
        public void GetMethodByKey_JiraOAuth_HasClientIdAndClientSecretOptions()
        {
            var method = AuthenticationMethodSchema.GetMethodByKey(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraOAuth);

            Assert.That(method, Is.Not.Null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(method!.Options, Has.Count.EqualTo(2));

                var clientId = method.Options.Single(o => o.Key == OAuthWorkTrackingOptionNames.ClientId);
                Assert.That(clientId.DisplayName, Is.EqualTo("Client ID"));
                Assert.That(clientId.IsSecret, Is.False);

                var clientSecret = method.Options.Single(o => o.Key == OAuthWorkTrackingOptionNames.ClientSecret);
                Assert.That(clientSecret.DisplayName, Is.EqualTo("Client Secret"));
                Assert.That(clientSecret.IsSecret, Is.True);
            }
        }

        [Test]
        public void GetOAuthProviderKeys_AfterJiraOAuthEntryAdded_ContainsJiraOAuth()
        {
            var oauthKeys = AuthenticationMethodSchema.GetOAuthProviderKeys();

            Assert.That(oauthKeys, Contains.Item(AuthenticationMethodKeys.JiraOAuth));
        }
    }
}
