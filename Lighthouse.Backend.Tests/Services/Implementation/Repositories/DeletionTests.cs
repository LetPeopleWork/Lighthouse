using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class DeletionTests : IntegrationTestBase
    {
        private WorkTrackingSystemConnection workTrackingSystemConnection;

        [SetUp]
        public void Setup()
        {
            workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
        }

        public DeletionTests() : base(new TestWebApplicationFactory<Program>())
        {
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

            var project = new Project { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var projectRepository = ServiceProvider.GetService<IRepository<Project>>();
            projectRepository.Add(project);            

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Order = "12",
            };

            feature.Projects.Add(project);

            feature.FeatureWork.Add(new FeatureWork(team, 12, 12, feature));
            project.Features.Add(feature);

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            // Act
            projectRepository.Remove(project.Id);
            await projectRepository.Save();

            Assert.Multiple(() =>
            {
                Assert.That(featureRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(projectRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task TeamInProject_DeleteProjectWithMilestones_DeletesFeaturesAndProjectAsync()
        {
            var team = new Team { Name = "MyTeam", WorkTrackingSystemConnection = workTrackingSystemConnection };
            team.UpdateThroughput([1, 0, 0, 2, 0, 1, 0, 0]);

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var project = new Project { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection, };

            var milestone1 = new Milestone { Name = "Milestone", Date = DateTime.Now.AddDays(12), Project = project };
            var milestone2 = new Milestone { Name = "Milestone2", Date = DateTime.Now.AddDays(42), Project = project };
            project.Milestones.Add(milestone1);
            project.Milestones.Add(milestone2);

            var projectRepository = ServiceProvider.GetService<IRepository<Project>>();
            projectRepository.Add(project);

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Order = "12",
            };

            feature.Projects.Add(project);

            feature.FeatureWork.Add(new FeatureWork(team, 12, 12, feature));
            project.Features.Add(feature);

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            var monteCarloService = ServiceProvider.GetService<IMonteCarloService>();
            await monteCarloService.ForecastAllFeatures();

            // Act
            projectRepository.Remove(project.Id);
            await projectRepository.Save();

            Assert.Multiple(() =>
            {
                Assert.That(featureRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(projectRepository.GetAll().ToList(), Has.Count.EqualTo(0));
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(1));
            });
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

            var project = new Project { Name = "MyProject", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var projectRepository = ServiceProvider.GetService<IRepository<Project>>();
            projectRepository.Add(project);

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Order = "12",
            };

            feature.Projects.Add(project);

            feature.FeatureWork.Add(new FeatureWork(team1, 12, 21, feature));
            feature.FeatureWork.Add(new FeatureWork(team2, 7, 42, feature));

            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            featureRepository.Add(feature);
            await featureRepository.Save();

            // Act
            teamRepository.Remove(team1.Id);
            await teamRepository.Save();

            Assert.Multiple(() =>
            {
                Assert.That(featureRepository.GetAll().ToList(), Has.Count.EqualTo(1));
                Assert.That(projectRepository.GetAll().ToList(), Has.Count.EqualTo(1));
                Assert.That(teamRepository.GetAll().ToList(), Has.Count.EqualTo(1));

                var feature = featureRepository.GetAll().Single();
                Assert.That(feature.FeatureWork, Has.Count.EqualTo(1));
            });
        }
    }
}
