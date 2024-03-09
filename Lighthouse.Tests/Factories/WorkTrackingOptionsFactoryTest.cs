using Lighthouse.Factories;
using Lighthouse.Models;
using Lighthouse.WorkTracking;
using Lighthouse.WorkTracking.AzureDevOps;
using Lighthouse.WorkTracking.Jira;

namespace Lighthouse.Tests.Factories
{
    public class WorkTrackingOptionsFactoryTest
    {
        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenUnknown_ReturnsCorrectOptions()
        {
            var subject = new WorkTrackingOptionsFactory();

            var options = subject.CreateOptionsForWorkTrackingSystem<Team>(WorkTrackingSystems.Unknown).ToList();

            Assert.That(options, Has.Count.EqualTo(0));
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenAzureDevOps_ReturnsCorrectOptions()
        {
            var subject = new WorkTrackingOptionsFactory();

            var options = subject.CreateOptionsForWorkTrackingSystem<Project>(WorkTrackingSystems.AzureDevOps);

            Assert.Multiple(() =>
            {
                Assert.That(options.Count(), Is.EqualTo(2));
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, true), Is.True);
            });
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenJira_ReturnsCorrectOptions()
        {
            var subject = new WorkTrackingOptionsFactory();

            var options = subject.CreateOptionsForWorkTrackingSystem<Project>(WorkTrackingSystems.Jira);

            Assert.Multiple(() =>
            {
                Assert.That(options.Count(), Is.EqualTo(3));
                Assert.That(ContainsOption(options, JiraWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(options, JiraWorkTrackingOptionNames.Username), Is.True);
                Assert.That(ContainsOption(options, JiraWorkTrackingOptionNames.ApiToken, true), Is.True);
            });
        }

        private bool ContainsOption<T>(IEnumerable<WorkTrackingSystemOption<T>> options, string key, bool isSecret = false) where T : class
        {
            return options.Any(option => option.Key == key && option.Secret == isSecret);
        }
    }
}
