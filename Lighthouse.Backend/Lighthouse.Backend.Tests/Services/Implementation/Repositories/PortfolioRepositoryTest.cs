using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class PortfolioRepositoryTest() : IntegrationTestBase
    {
        [Test]
        public async Task GetProjectById_ProjectHasInvolvedTeams_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Connection"
            };

            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Key", Value = "Value" });


            var project = new Portfolio { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var team = new Team { Name = "Team1", WorkTrackingSystemConnection = workTrackingSystemConnection };

            DatabaseContext.Teams.Add(team);
            await DatabaseContext.SaveChangesAsync();

            var feature = new Feature { Name = "Feature", Order = "12" };
            feature.FeatureWork.Add(new FeatureWork(team, 12, 37, feature));

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundProject, Is.EqualTo(project));
                Assert.That(foundProject.Teams.ToList(), Has.Count.EqualTo(1));
                Assert.That(foundProject.Teams.Single().WorkTrackingSystemConnection.Options, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task GetProjectById_ProjectHasFeatures_LoadsCorrectlyFromDatabase()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Connection"
            };

            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Key", Value = "Value" });

            var project = new Portfolio { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var feature = new Feature { Name = "MyFeature", Order = "12" };

            project.Features.Add(feature);

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundProject, Is.EqualTo(project));
                Assert.That(foundProject.Features, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task AddOverrideSetting_SavesCorrectly()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Connection"
            };

            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Key", Value = "Value" });

            var project = new Portfolio { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            project.OverrideRealChildCountStates.Add("New");
            project.OverrideRealChildCountStates.Add("AnalysisInProgress");

            // Act
            subject.Add(project);
            await subject.Save();

            var foundProject = subject.GetById(project.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundProject.OverrideRealChildCountStates, Has.Count.EqualTo(2));
                Assert.That(foundProject.OverrideRealChildCountStates, Does.Contain("New"));
                Assert.That(foundProject.OverrideRealChildCountStates, Does.Contain("AnalysisInProgress"));
            }
        }

        [Test]
        public async Task GetPortfolioIdsForTeam_TeamLinkedOnlyThroughFeatureWork_ReturnsPortfolio()
        {
            var subject = CreateSubject();

            var connection = CreateConnection();
            var team = new Team { Name = "Team1", WorkTrackingSystemConnection = connection };
            DatabaseContext.Teams.Add(team);
            await DatabaseContext.SaveChangesAsync();

            var portfolio = new Portfolio { Name = "Portfolio", WorkTrackingSystemConnection = connection };
            var feature = new Feature { Name = "Feature", Order = "12" };
            feature.FeatureWork.Add(new FeatureWork(team, 12, 37, feature));
            portfolio.Features.Add(feature);

            subject.Add(portfolio);
            await subject.Save();
            var portfolioId = portfolio.Id;

            DatabaseContext.ChangeTracker.Clear();

            var portfolioIds = subject.GetPortfolioIdsForTeam(team.Id);

            Assert.That(portfolioIds, Is.EqualTo(new[] { portfolioId }));
        }

        [Test]
        public async Task GetPortfolioIdsForTeam_TeamWorksOnSeveralFeaturesOfSamePortfolio_ReturnsPortfolioOnce()
        {
            var subject = CreateSubject();

            var connection = CreateConnection();
            var team = new Team { Name = "Team1", WorkTrackingSystemConnection = connection };
            DatabaseContext.Teams.Add(team);
            await DatabaseContext.SaveChangesAsync();

            var portfolio = new Portfolio { Name = "Portfolio", WorkTrackingSystemConnection = connection };

            foreach (var featureName in new[] { "First", "Second" })
            {
                var feature = new Feature { Name = featureName, Order = "12" };
                feature.FeatureWork.Add(new FeatureWork(team, 1, 2, feature));
                portfolio.Features.Add(feature);
            }

            subject.Add(portfolio);
            await subject.Save();

            var portfolioIds = subject.GetPortfolioIdsForTeam(team.Id);

            Assert.That(portfolioIds, Is.EqualTo(new[] { portfolio.Id }));
        }

        [Test]
        public async Task GetPortfolioIdsForTeam_TeamWorksOnNoneOfThePortfoliosFeatures_ReturnsNothing()
        {
            var subject = CreateSubject();

            var connection = CreateConnection();
            var team = new Team { Name = "Team1", WorkTrackingSystemConnection = connection };
            var otherTeam = new Team { Name = "Team2", WorkTrackingSystemConnection = connection };
            DatabaseContext.Teams.AddRange(team, otherTeam);
            await DatabaseContext.SaveChangesAsync();

            var portfolio = new Portfolio { Name = "Portfolio", WorkTrackingSystemConnection = connection };
            var feature = new Feature { Name = "Feature", Order = "12" };
            feature.FeatureWork.Add(new FeatureWork(otherTeam, 12, 37, feature));
            portfolio.Features.Add(feature);

            subject.Add(portfolio);
            await subject.Save();

            var portfolioIds = subject.GetPortfolioIdsForTeam(team.Id);

            Assert.That(portfolioIds, Is.Empty);
        }

        [Test]
        public void PortfolioRepository_ResolvedThroughEitherPort_IsTheSameScopedInstance()
        {
            var portfolioPort = ServiceProvider.GetRequiredService<IPortfolioRepository>();
            var genericPort = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            Assert.That(genericPort, Is.SameAs(portfolioPort));
        }

        private static WorkTrackingSystemConnection CreateConnection()
        {
            return new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Connection"
            };
        }

        private PortfolioRepository CreateSubject()
        {
            return new PortfolioRepository(DatabaseContext, Mock.Of<ILogger<PortfolioRepository>>());
        }
    }
}
