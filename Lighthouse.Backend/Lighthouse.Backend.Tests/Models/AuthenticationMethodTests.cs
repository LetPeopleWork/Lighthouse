using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Models
{
    public class AuthenticationMethodTests
    {
        [Test]
        public void WorkTrackingSystemConnection_HasAuthenticationMethodKeyProperty()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps
            };

            // Should be able to set and get AuthenticationMethodKey
            connection.AuthenticationMethodKey = "ado.pat";

            Assert.That(connection.AuthenticationMethodKey, Is.EqualTo("ado.pat"));
        }

        [Test]
        public void WorkTrackingSystemConnection_AuthenticationMethodKey_DefaultsToEmptyString()
        {
            var connection = new WorkTrackingSystemConnection();

            Assert.That(connection.AuthenticationMethodKey, Is.EqualTo(string.Empty));
        }

        [Test]
        [TestCase(WorkTrackingSystems.AzureDevOps, "ado.pat")]
        [TestCase(WorkTrackingSystems.Linear, "linear.apikey")]
        [TestCase(WorkTrackingSystems.Csv, "none")]
        public void WorkTrackingSystemConnection_AuthenticationMethodKey_CanBeSetToValidKeys(
            WorkTrackingSystems system, string expectedKey)
        {
            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = system,
                AuthenticationMethodKey = expectedKey
            };

            Assert.That(connection.AuthenticationMethodKey, Is.EqualTo(expectedKey));
        }

        [Test]
        [TestCase(WorkTrackingSystems.Jira, "jira.cloud")]
        [TestCase(WorkTrackingSystems.Jira, "jira.datacenter")]
        public void WorkTrackingSystemConnection_Jira_SupportsMultipleAuthMethods(
            WorkTrackingSystems system, string methodKey)
        {
            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = system,
                AuthenticationMethodKey = methodKey
            };

            Assert.That(connection.AuthenticationMethodKey, Is.EqualTo(methodKey));
        }
    }
}
