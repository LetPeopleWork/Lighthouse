using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
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
            team.RawThroughput = rawThroughput;

            Assert.That(team.RawThroughput, Is.EqualTo(rawThroughput));
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
    }
}
