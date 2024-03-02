using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
{
    public class DeletionTests : IntegrationTestBase
    {

        public DeletionTests() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task TeamWithoutProjects_DeleteWorksAsync()
        {
            var team = new Team { Name = "MyTeam" };

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
            var team = new Team { Name = "MyTeam" };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var project = new Project { Name = "MyProject", SearchTerm = "Search" };
            project.InvolvedTeams.Add(new TeamInProject(team, project));

            var projectRepository = ServiceProvider.GetService<IRepository<Project>>();
            projectRepository.Add(project);            

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Project = project,
                ProjectId = project.Id,
            };

            feature.RemainingWork.Add(new RemainingWork(team, 12, feature));
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
            var team = new Team { Name = "MyTeam" };
            team.UpdateThroughput([1, 0, 0, 2, 0, 1, 0, 0]);

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var project = new Project { Name = "MyProject", SearchTerm = "Search" };
            project.InvolvedTeams.Add(new TeamInProject(team, project));

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
                Project = project,
                ProjectId = project.Id,
            };

            feature.RemainingWork.Add(new RemainingWork(team, 12, feature));
            project.Features.Add(feature);

            var monteCarloService = ServiceProvider.GetService<IMonteCarloService>();
            monteCarloService.ForecastFeatures(project.Features);

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
        public async Task TeamsInProject_DeleteTeam_DeletesRemainingWorkOfTeamAsync()
        {
            var team1 = new Team { Name = "MyTeam1" };
            var team2 = new Team { Name = "MyTeam2" };

            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team1);
            teamRepository.Add(team2);
            await teamRepository.Save();

            var project = new Project { Name = "MyProject", SearchTerm = "Search" };
            project.InvolvedTeams.Add(new TeamInProject(team1, project));
            project.InvolvedTeams.Add(new TeamInProject(team2, project));

            var projectRepository = ServiceProvider.GetService<IRepository<Project>>();
            projectRepository.Add(project);

            await projectRepository.Save();

            var feature = new Feature
            {
                Name = "My Feature",
                Project = project,
                ProjectId = project.Id,
            };

            feature.RemainingWork.Add(new RemainingWork(team1, 12, feature));
            feature.RemainingWork.Add(new RemainingWork(team2, 7, feature));

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
                Assert.That(feature.RemainingWork, Has.Count.EqualTo(1));
            });
        }
    }
}
