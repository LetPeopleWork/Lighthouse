using Castle.Core.Logging;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
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
            return new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
        }
    }
}
