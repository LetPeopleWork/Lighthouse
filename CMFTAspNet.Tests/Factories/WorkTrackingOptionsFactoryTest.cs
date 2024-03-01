using CMFTAspNet.Factories;
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

            var options = subject.CreateOptionsForWorkTrackingSystem(WorkTrackingSystems.Unknown).ToList();

            Assert.That(options, Has.Count.EqualTo(0));
        }

        [Test]
        public void CreateOptionsForWorkTrackingSystem_GivenAzureDevOps_ReturnsCorrectOptions()
        {
            var subject = new WorkTrackingOptionsFactory();

            var options = subject.CreateOptionsForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            Assert.Multiple(() =>
            {
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.AzureDevOpsUrl), Is.True);
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.AzureDevOpsTeamProject), Is.True);
                Assert.That(ContainsOption(options, AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, true), Is.True);
            });
        }

        private bool ContainsOption(IEnumerable<WorkTrackingSystemOption> options, string key, bool isSecret = false)
        {
            return options.Any(option => option.Key == key && option.Secret == isSecret);
        }
    }
}
