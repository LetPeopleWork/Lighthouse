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

        [Test]
        public async Task Remove_TeamWithFeatureWork_RemovesTeamAndFeatureWork()
        {
            var subject = CreateSubject();
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };

            var team = new Team { Name = "Team To Delete", WorkTrackingSystemConnection = workTrackingSystemConnection };
            subject.Add(team);
            await subject.Save();

            var portfolio = new Portfolio
            {
                Name = "Portfolio",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "PortfolioConnection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps }
            };
            portfolio.Teams.Add(team);
            DatabaseContext.Portfolios.Add(portfolio);
            await DatabaseContext.SaveChangesAsync();

            var feature = new Feature { ReferenceId = "F1", Name = "Feature 1", Order = "1" };
            feature.Portfolios.Add(portfolio);
            feature.AddOrUpdateWorkForTeam(team, 3, 5);
            DatabaseContext.Features.Add(feature);
            await DatabaseContext.SaveChangesAsync();

            // Verify FeatureWork exists
            var featureWorkCount = DatabaseContext.Set<FeatureWork>().Count(fw => fw.TeamId == team.Id);
            Assert.That(featureWorkCount, Is.GreaterThan(0));

            // Now delete the team
            subject.Remove(team.Id);
            await subject.Save();

            // Team should be gone
            var foundTeam = subject.GetById(team.Id);
            Assert.That(foundTeam, Is.Null);

            // FeatureWork for that team should also be gone
            var remainingFeatureWork = DatabaseContext.Set<FeatureWork>().Count(fw => fw.TeamId == team.Id);
            Assert.That(remainingFeatureWork, Is.Zero);
        }

        private TeamRepository CreateSubject()
        {
            return new TeamRepository(DatabaseContext, Mock.Of<ILogger<TeamRepository>>());
        }
    }
}
