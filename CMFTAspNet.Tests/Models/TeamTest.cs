using CMFTAspNet.Models;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Tests.Models
{
    public class TeamTest
    {
        [Test]
        public void UpdateThroughput_SetsThroughput()
        {
            var team = new Team();

            var rawThroughput = new int[] { 1, 3, 0, 0, 0, 1, 3 };
            team.UpdateThroughput(rawThroughput);

            Assert.That(team.Throughput.ThroughputPerUnitOfTime, Is.EqualTo(rawThroughput));
        }

        [Test]
        public void CreateNewTeam_InitializesWithUnknownWorkTrackingSystem()
        {
            var team = new Team
            {
                Name = "Test",
            };

            Assert.That(team.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Unknown));
        }

        [Test]
        public void GetWorkTrackingOptionByKey_ValidKey_ReturnsOption()
        {
            var team = new Team();
            var workTrackingOption = new WorkTrackingSystemOption<Team>("Key", "Value", false);
            team.WorkTrackingSystemOptions.Add(workTrackingOption);

            var actualValue = team.GetWorkTrackingSystemOptionByKey("Key");

            Assert.That(actualValue, Is.EqualTo("Value"));
        }

        [Test]
        public void GetWorkTrackingOptionByKey_InvalidKey_Throws()
        {
            var team = new Team();
            var workTrackingOption = new WorkTrackingSystemOption<Team>("Key", "Value", false);
            team.WorkTrackingSystemOptions.Add(workTrackingOption);

            Assert.Throws<ArgumentException>(() => team.GetWorkTrackingSystemOptionByKey("InvalidKey"));
        }
    }
}
