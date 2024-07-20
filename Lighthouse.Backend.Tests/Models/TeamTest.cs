using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
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
    }
}
