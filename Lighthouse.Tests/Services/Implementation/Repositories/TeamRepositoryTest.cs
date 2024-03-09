using Lighthouse.Models;
using Lighthouse.Services.Implementation.Repositories;
using Lighthouse.Tests.TestHelpers;
using Lighthouse.WorkTracking;

namespace Lighthouse.Tests.Services.Implementation.Repositories
{
    public class TeamRepositoryTest : IntegrationTestBase
    {
        public TeamRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task GetTeamById_ExistingId_RetunrsCorrectTeam()
        {
            var subject = CreateSubject();
            var workTrackingOptions = new List<WorkTrackingSystemOption<Team>> { new WorkTrackingSystemOption<Team>("key", "value", false) };
            var team = new Team {  Name = "Name", WorkTrackingSystemOptions = workTrackingOptions };

            subject.Add(team);
            await subject.Save();

            var foundTeam = subject.GetById(team.Id);

            Assert.That(foundTeam, Is.EqualTo(team));
            CollectionAssert.AreEquivalent(foundTeam.WorkTrackingSystemOptions, workTrackingOptions);
        }

        private TeamRepository CreateSubject()
        {
            return new TeamRepository(DatabaseContext);
        }
    }
}
