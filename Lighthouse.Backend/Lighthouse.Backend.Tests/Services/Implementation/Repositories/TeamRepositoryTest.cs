using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
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
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "key", Value = "value" });

            var team = new Team { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            subject.Add(team);
            await subject.Save();

            var foundTeam = subject.GetById(team.Id);

            Assert.That(foundTeam, Is.EqualTo(team));
            Assert.That(foundTeam.WorkTrackingSystemConnection, Is.EqualTo(workTrackingSystemConnection));
        }

        private TeamRepository CreateSubject()
        {
            return new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
        }
    }
}
