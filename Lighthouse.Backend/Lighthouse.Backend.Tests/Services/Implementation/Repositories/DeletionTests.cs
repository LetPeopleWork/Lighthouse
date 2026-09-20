using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class DeletionTests() : IntegrationTestBase
    {
        private WorkTrackingSystemConnection workTrackingSystemConnection;

        [SetUp]
        public void Setup()
        {
            workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
        }

        [Test]
        public async Task TeamWithoutProjects_DeleteWorksAsync()
        {
            var team = new Team { Name = "MyTeam", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();

            teamRepository.Add(team);
            await teamRepository.Save();

            teamRepository.Remove(team.Id);

            await teamRepository.Save();

            Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(0));
        }

        [Test]
        public async Task TeamInProject_DeleteProject_DeletesFeaturesAndProjectAsync()
        {
            var team = new Team { Name = "MyTeam", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var project = new Portfolio { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var projectRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            projectRepository.Add(project);            

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Order = "12",
            };

            feature.Portfolios.Add(project);

            feature.FeatureWork.Add(new FeatureWork(team, 12, 12, feature));
            project.Features.Add(feature);

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            // Act
            projectRepository.Remove(project.Id);
            await projectRepository.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(projectRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(1));
            };
        }

        [Test]
        public async Task TeamInProject_DeleteProjectWithMilestones_DeletesFeaturesAndProjectAsync()
        {
            var team = new Team { Name = "MyTeam", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var project = new Portfolio { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection, };


            var projectRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            projectRepository.Add(project);

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Order = "12",
            };

            feature.Portfolios.Add(project);

            feature.FeatureWork.Add(new FeatureWork(team, 12, 12, feature));
            project.Features.Add(feature);

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            var forecastUpdateService = ServiceProvider.GetService<IForecastUpdater>();
            forecastUpdateService.TriggerUpdate(project.Id);

            // Act
            projectRepository.Remove(project.Id);
            await projectRepository.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(projectRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(1));
            };
        }

        [Test]
        public async Task TeamsInProject_DeleteTeam_DeletesRemainingWorkOfTeamAsync()
        {
            var team1 = new Team { Name = "MyTeam1", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var team2 = new Team { Name = "MyTeam2", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team1);
            teamRepository.Add(team2);
            await teamRepository.Save();

            var project = new Portfolio { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var projectRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            projectRepository.Add(project);

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Order = "12",
            };

            feature.Portfolios.Add(project);

            feature.FeatureWork.Add(new FeatureWork(team1, 12, 21, feature));
            feature.FeatureWork.Add(new FeatureWork(team2, 7, 42, feature));

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            teamRepository.Remove(team1.Id);
            await teamRepository.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureRepository.GetAll().ToList(), Has.Count.EqualTo(1));
                Assert.That(projectRepository.GetAll().ToList(), Has.Count.EqualTo(1));
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(1));

                var featureToVerify = featureRepository.GetAll().Single();
                Assert.That(featureToVerify.FeatureWork, Has.Count.EqualTo(1));
            };
        }

        [Test]
        public async Task TeamInProject_WithExistingForecasts_DeleteTeam_SucceedsAsync()
        {
            var team = new Team { Name = "MyTeam", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var portfolio = new Portfolio { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var projectRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            projectRepository.Add(portfolio);
            await projectRepository.Save();

            var feature = new Feature { Name = "My Feature", Order = "12" };
            feature.Portfolios.Add(portfolio);
            portfolio.Features.Add(feature);

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            var whenForecast = new WhenForecast { FeatureId = feature.Id, TeamId = team.Id, NumberOfItems = 5 };
            DatabaseContext.Set<WhenForecast>().Add(whenForecast);
            await DatabaseContext.SaveChangesAsync();

            // Clear the change tracker to simulate a fresh request context (the real bug scenario:
            // the WhenForecast was created by a prior portfolio-update request; the team deletion
            // is a new request with no prior tracked entities).
            DatabaseContext.ChangeTracker.Clear();

            teamRepository.Remove(team.Id);
            await teamRepository.Save();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                var remaining = DatabaseContext.Set<WhenForecast>().AsNoTracking().FirstOrDefault(wf => wf.Id == whenForecast.Id);
                Assert.That(remaining?.TeamId, Is.Null);
            }
        }

        /// <summary>
        /// Start forecasts go with the team rather than being orphaned, and the test is about what the
        /// Feature says afterwards rather than about the row count.
        ///
        /// A Feature's own start is the row whose team is null, because no single team owns it. Nulling a
        /// per-team row's team on deletion - which is what happens to the completion row above - would
        /// produce a second row that looks exactly like it, and the Feature would report one team's start
        /// as its own. Silently, with no error, until some later forecast happened to rewrite the rows.
        /// </summary>
        [Test]
        public async Task TeamInProject_WithExistingStartForecasts_DeleteTeam_LeavesTheFeaturesOwnStartAlone()
        {
            const int theDayThatTeamWouldHaveStarted = 2;
            const int theDayTheFeatureStarts = 9;

            var team = new Team { Name = "MyTeam", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var portfolio = new Portfolio { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection };
            var projectRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            projectRepository.Add(portfolio);
            await projectRepository.Save();

            var feature = new Feature { Name = "My Feature", Order = "12" };
            feature.Portfolios.Add(portfolio);
            portfolio.Features.Add(feature);

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            // The team's own row first and the Feature's own row second, which is the order a forecast
            // writes them in and therefore the order they come back in.
            DatabaseContext.Set<StartForecast>().Add(
                new StartForecast(new Dictionary<int, int> { [theDayThatTeamWouldHaveStarted] = 10 }) { FeatureId = feature.Id, TeamId = team.Id });
            DatabaseContext.Set<StartForecast>().Add(
                new StartForecast(new Dictionary<int, int> { [theDayTheFeatureStarts] = 10 }) { FeatureId = feature.Id });
            await DatabaseContext.SaveChangesAsync();

            DatabaseContext.ChangeTracker.Clear();

            teamRepository.Remove(team.Id);
            await teamRepository.Save();

            DatabaseContext.ChangeTracker.Clear();

            var afterTheDeletion = featureRepository.GetAll().Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheDeletion.StartForecasts, Has.Count.EqualTo(1),
                    "the deleted team's row goes with it rather than being left behind with no team");
                Assert.That(afterTheDeletion.WhenWorkBegins.Forecast?.GetProbability(85), Is.EqualTo(theDayTheFeatureStarts),
                    "the Feature still reports its own start, not the start of the team that was removed");
            }
        }
    }
}
