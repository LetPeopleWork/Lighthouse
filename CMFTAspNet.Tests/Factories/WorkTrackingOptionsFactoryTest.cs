using CMFTAspNet.Factories;
using CMFTAspNet.Models;
using CMFTAspNet.WorkTracking;
using CMFTAspNet.WorkTracking.AzureDevOps;

namespace CMFTAspNet.Tests.Factories
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
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.Url), Is.True);
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.TeamProject), Is.True);
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, true), Is.True);
            });
        }

        private bool ContainsOption<T>(IEnumerable<WorkTrackingSystemOption<T>> options, string key, bool isSecret = false) where T : class
        {
            return options.Any(option => option.Key == key && option.Secret == isSecret);
        }
    }
}
