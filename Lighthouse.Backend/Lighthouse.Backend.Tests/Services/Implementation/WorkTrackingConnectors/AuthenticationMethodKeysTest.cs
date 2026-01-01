using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors
{
    public class AuthenticationMethodKeysTest
    {
        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps, AuthenticationMethodKeys.AzureDevOpsPat)]
        [TestCase(WorkTrackingSystems.Jira, AuthenticationMethodKeys.JiraCloud)]
        [TestCase(WorkTrackingSystems.Linear, AuthenticationMethodKeys.LinearApiKey)]
        [TestCase(WorkTrackingSystems.Csv, AuthenticationMethodKeys.None)]
        public void GetDefaultForSystem_ReturnsExpectedKey(WorkTrackingSystems system, string expectedKey)
        {
            var result = AuthenticationMethodKeys.GetDefaultForSystem(system);

            Assert.That(result, Is.EqualTo(expectedKey));
        }

        [Test]
        public void AuthenticationMethodKeys_AreStable()
        {
            // These keys must never change - they're stored in databases and exports
            Assert.That(AuthenticationMethodKeys.AzureDevOpsPat, Is.EqualTo("ado.pat"));
            Assert.That(AuthenticationMethodKeys.JiraCloud, Is.EqualTo("jira.cloud"));
            Assert.That(AuthenticationMethodKeys.JiraDataCenter, Is.EqualTo("jira.datacenter"));
            Assert.That(AuthenticationMethodKeys.LinearApiKey, Is.EqualTo("linear.apikey"));
            Assert.That(AuthenticationMethodKeys.None, Is.EqualTo("none"));
        }
    }
}
