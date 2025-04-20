using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    public class WorkTrackingSystemFactoryTest
    {
        [Test]
        [TestCase(WorkTrackingSystems.Jira)]
        [TestCase(WorkTrackingSystems.AzureDevOps)]
        public void CreateDefaultConnectionForWorkTrackingSystem_CreatesDefaultConnection(WorkTrackingSystems workTrackingSystem)
        {
            var subject = CreateSubject();

            var defaultConnection = subject.CreateDefaultConnectionForWorkTrackingSystem(workTrackingSystem);

            Assert.Multiple(() =>
            {
                Assert.That(defaultConnection.Id, Is.EqualTo(0));
                Assert.That(defaultConnection.Name, Is.EqualTo($"New {workTrackingSystem.ToString()} Connection"));
                Assert.That(defaultConnection.WorkTrackingSystem, Is.EqualTo(workTrackingSystem));
            });
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenAzureDevOps_ReturnsCorrectOptions()
        {
            var subject = CreateSubject();

            var connection = subject.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            Assert.Multiple(() =>
            {
                Assert.That(connection.Options, Has.Count.EqualTo(2));
                Assert.That(ContainsOption(connection.Options, AzureDevOpsWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(connection.Options, AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, true), Is.True);
            });
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenJira_ReturnsCorrectOptions()
        {
            var subject = CreateSubject();

            var connection = subject.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Jira);

            Assert.Multiple(() =>
            {
                Assert.That(connection.Options, Has.Count.EqualTo(3));
                Assert.That(ContainsOption(connection.Options, JiraWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(connection.Options, JiraWorkTrackingOptionNames.Username, false, true), Is.True);
                Assert.That(ContainsOption(connection.Options, JiraWorkTrackingOptionNames.ApiToken, true), Is.True);
            });
        }

        private bool ContainsOption(IEnumerable<WorkTrackingSystemConnectionOption> options, string key, bool isSecret = false, bool isOptional = false)
        {
            return options.Any(option => option.Key == key && option.IsSecret == isSecret && option.IsOptional == isOptional);
        }

        private WorkTrackingSystemFactory CreateSubject()
        {
            return new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>());
        }
    }
}
