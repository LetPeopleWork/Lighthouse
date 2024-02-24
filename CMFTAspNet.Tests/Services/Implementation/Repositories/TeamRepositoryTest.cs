using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;
using CMFTAspNet.Tests.TestHelpers;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
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
            var workTrackingOptions = new List<WorkTrackingSystemOption> { new WorkTrackingSystemOption("key", "value") };
            var team = new Team {  Name = "Name", ProjectName = "Project", WorkTrackingSystemOptions = workTrackingOptions };

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
