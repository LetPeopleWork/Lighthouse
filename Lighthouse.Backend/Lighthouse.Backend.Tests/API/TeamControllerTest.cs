using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class TeamControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IWorkItemRepository> workItemRepoMock;

        private Mock<ITeamUpdater> teamUpdateServiceMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            workItemRepoMock = new Mock<IWorkItemRepository>();
            teamUpdateServiceMock = new Mock<ITeamUpdater>();
        }

        [Test]
        public async Task Delete_RemovesTeamAndSaves()
        {
            const int teamId = 12;

            var subject = CreateSubject();

            await subject.DeleteTeam(teamId);

            teamRepositoryMock.Verify(x => x.Remove(teamId));
            teamRepositoryMock.Verify(x => x.Save());
        }

        [Test]
        public void GetTeam_TeamExists_ReturnsTeam()
        {
            var team = CreateTeam(1, "Numero Uno");

            var portfolio1 = CreatePortfolio(42, "My Portfolio");
            portfolio1.UpdateTeams([team]);

            var feature1 = CreateFeature(portfolio1, team, 12);
            var feature2 = CreateFeature(portfolio1, team, 42);

            var portfolio2 = CreatePortfolio(13, "My Other Portfolio");
            portfolio2.UpdateTeams([team]);


            var feature3 = CreateFeature(portfolio2, team, 5);

            teamRepositoryMock.Setup(x => x.GetById(1)).Returns(team);

            var subject = CreateSubject([team], [portfolio1, portfolio2], [feature1, feature2, feature3]);

            var result = subject.GetTeam(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var returnedTeamDto = okResult.Value as TeamDto;

                Assert.That(returnedTeamDto.Id, Is.EqualTo(1));
                Assert.That(returnedTeamDto.Name, Is.EqualTo("Numero Uno"));
                Assert.That(returnedTeamDto.Portfolios, Has.Count.EqualTo(2));
                Assert.That(returnedTeamDto.Features, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public void GetTeam_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.GetTeam(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void GetTeamSettings_TeamExists_ReturnsSettings()
        {
            var team = CreateTeam(12, "El Teamo");
            team.ThroughputHistory = 42;
            team.FeatureWIP = 3;
            team.DataRetrievalValue = "SELECT * FROM *";
            team.WorkTrackingSystemConnectionId = 37;
            team.ParentOverrideAdditionalFieldDefinitionId = 5;
            team.ServiceLevelExpectationProbability = 75;
            team.ServiceLevelExpectationRange = 10;
            team.SystemWIPLimit = 5;

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            var result = subject.GetTeamSettings(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<TeamSettingDto>());
                var teamSettingDto = okObjectResult.Value as TeamSettingDto;

                Assert.That(teamSettingDto.Id, Is.EqualTo(team.Id));
                Assert.That(teamSettingDto.Name, Is.EqualTo(team.Name));
                Assert.That(teamSettingDto.ThroughputHistory, Is.EqualTo(team.ThroughputHistory));
                Assert.That(teamSettingDto.FeatureWIP, Is.EqualTo(team.FeatureWIP));
                Assert.That(teamSettingDto.DataRetrievalValue, Is.EqualTo(team.DataRetrievalValue));
                Assert.That(teamSettingDto.WorkItemTypes, Is.EqualTo(team.WorkItemTypes));
                Assert.That(teamSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(team.WorkTrackingSystemConnectionId));
                Assert.That(teamSettingDto.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(team.ParentOverrideAdditionalFieldDefinitionId));
                Assert.That(teamSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(team.ServiceLevelExpectationProbability));
                Assert.That(teamSettingDto.ServiceLevelExpectationRange, Is.EqualTo(team.ServiceLevelExpectationRange));
                Assert.That(teamSettingDto.SystemWIPLimit, Is.EqualTo(team.SystemWIPLimit));
            }
        }

        [Test]
        public void GetTeamSettings_TeamNotFound_ReturnsNotFoundResult()
        {
            var subject = CreateSubject();

            var result = subject.GetTeamSettings(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task UpdateTeam_GivenNewTeamSettings_UpdatesTeamAsync()
        {
            var existingTeam = new Team { Id = 132 };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                DataRetrievalValue = "project = MyProject",
                WorkItemTypes = ["User Story", "Bug"],
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideAdditionalFieldDefinitionId = 7,
                AutomaticallyAdjustFeatureWIP = true,
                ServiceLevelExpectationRange = 18,
                ServiceLevelExpectationProbability = 86,
                BlockedStates = ["Waiting for Peter"],
                BlockedTags = ["Customer Input Needed"],
            };

            var subject = CreateSubject();

            var result = await subject.UpdateTeam(132, updatedTeamSettings);

            teamRepositoryMock.Verify(x => x.Update(existingTeam));
            teamRepositoryMock.Verify(x => x.Save());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<TeamSettingDto>());
                var teamSettingDto = okObjectResult.Value as TeamSettingDto;

                Assert.That(teamSettingDto.Name, Is.EqualTo(updatedTeamSettings.Name));
                Assert.That(teamSettingDto.ThroughputHistory, Is.EqualTo(updatedTeamSettings.ThroughputHistory));
                Assert.That(teamSettingDto.FeatureWIP, Is.EqualTo(updatedTeamSettings.FeatureWIP));
                Assert.That(teamSettingDto.DataRetrievalValue, Is.EqualTo(updatedTeamSettings.DataRetrievalValue));
                Assert.That(teamSettingDto.WorkItemTypes, Is.EqualTo(updatedTeamSettings.WorkItemTypes));
                Assert.That(teamSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(updatedTeamSettings.WorkTrackingSystemConnectionId));
                Assert.That(teamSettingDto.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(updatedTeamSettings.ParentOverrideAdditionalFieldDefinitionId));
                Assert.That(teamSettingDto.AutomaticallyAdjustFeatureWIP, Is.EqualTo(updatedTeamSettings.AutomaticallyAdjustFeatureWIP));
                Assert.That(teamSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(updatedTeamSettings.ServiceLevelExpectationProbability));
                Assert.That(teamSettingDto.ServiceLevelExpectationRange, Is.EqualTo(updatedTeamSettings.ServiceLevelExpectationRange));
                Assert.That(teamSettingDto.BlockedStates, Contains.Item("Waiting for Peter"));
                Assert.That(teamSettingDto.BlockedTags, Contains.Item("Customer Input Needed"));
            }
        }

        [Test]
        [TestCase("New Query", true)]
        [TestCase("Existing Query", false)]
        public async Task UpdateTeam_GivenNewQuery_DeletesExistingWorkItems(string workItemQuery, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, DataRetrievalValue = "Existing Query", WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                DataRetrievalValue = workItemQuery,
                ToDoStates = existingTeam.ToDoStates,
                DoingStates = existingTeam.DoingStates,
                DoneStates = existingTeam.DoneStates,
                WorkItemTypes = ["User Story", "Bug"],
                WorkTrackingSystemConnectionId = 2,
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        [TestCase(new[] { "User Story", "Bug" }, false)]
        [TestCase(new[] { "Bug", "User Story" }, false)]
        [TestCase(new[] { "Story", "Bug" }, true)]
        [TestCase(new[] { "User Story" }, true)]
        [TestCase(new[] { "All New Type" }, true)]
        [TestCase(new[] { "User Story", "Bug", "Task" }, true)]
        public async Task UpdateTeam_GivenWorkItemTypes_DeletesExistingWorkItems(string[] workItemTypes, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, DataRetrievalValue = "Existing Query", WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                DataRetrievalValue = "Existing Query",
                ToDoStates = existingTeam.ToDoStates,
                DoingStates = existingTeam.DoingStates,
                DoneStates = existingTeam.DoneStates,
                WorkItemTypes = [.. workItemTypes],
                WorkTrackingSystemConnectionId = 2,
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        [TestCase(new[] { "To Do" }, new[] { "Doing" }, new[] { "Done" }, false)]
        [TestCase(new[] { "ToDo" }, new[] { "Doing" }, new[] { "Done" }, true)]
        [TestCase(new[] { "To Do" }, new[] { "Boing" }, new[] { "Done" }, true)]
        [TestCase(new[] { "To Do" }, new[] { "Doing" }, new[] { "Donny" }, true)]
        [TestCase(new[] { "To Do", "New" }, new[] { "Doing" }, new[] { "Done" }, true)]
        [TestCase(new[] { "To Do" }, new[] { "Doing", "In Progress" }, new[] { "Done" }, true)]
        [TestCase(new[] { "To Do" }, new[] { "Doing" }, new[] { "Done", "Closed" }, true)]
        public async Task UpdateTeam_GivenChangedStates_DeletesExistingWorkItems(string[] toDoStates, string[] doingStates, string[] doneStates, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, DataRetrievalValue = "Existing Query", ToDoStates = ["To Do"], DoingStates = ["Doing"], DoneStates = ["Done"], WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                DataRetrievalValue = "Existing Query",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = toDoStates.ToList(),
                DoingStates = doingStates.ToList(),
                DoneStates = doneStates.ToList(),
                WorkTrackingSystemConnectionId = 2,
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        [TestCase(2, false)]
        [TestCase(1, true)]
        public async Task UpdateTeam_GivenWorkTrackingSystemConnectionId_DeletesExistingWorkItems(int workTrackingSystemConnectionId, bool shouldDelete)
        {
            var existingTeam = new Team { Id = 132, DataRetrievalValue = "Existing Query", WorkItemTypes = ["User Story", "Bug"], WorkTrackingSystemConnectionId = 2, UpdateTime = DateTime.UtcNow };

            teamRepositoryMock.Setup(x => x.GetById(132)).Returns(existingTeam);

            var updatedTeamSettings = new TeamSettingDto
            {
                Id = 132,
                Name = "Updated Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                DataRetrievalValue = "Existing Query",
                ToDoStates = existingTeam.ToDoStates,
                DoingStates = existingTeam.DoingStates,
                DoneStates = existingTeam.DoneStates,
                WorkItemTypes = ["User Story", "Bug"],
                WorkTrackingSystemConnectionId = workTrackingSystemConnectionId,
                AutomaticallyAdjustFeatureWIP = true,
            };

            var subject = CreateSubject();

            _ = await subject.UpdateTeam(132, updatedTeamSettings);

            workItemRepoMock.Verify(x => x.RemoveWorkItemsForTeam(existingTeam.Id), shouldDelete ? Times.Once : Times.Never);
        }

        [Test]
        public async Task UpdateTeam_TeamNotFound_ReturnsNotFoundResultAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdateTeam(1, new TeamSettingDto());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void UpdateTeamData_GivenTeamId_TriggersTeamUpdate()
        {
            var expectedTeam = new Team();
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(expectedTeam);

            var subject = CreateSubject();

            var response = subject.UpdateTeamData(12);

            teamUpdateServiceMock.Verify(x => x.TriggerUpdate(expectedTeam.Id));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());
                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            }
        }

        [Test]
        public void UpdateTeamData_TeamDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = subject.UpdateTeamData(12);

            Assert.That(response, Is.InstanceOf<NotFoundObjectResult>());
            var notFoundObjectResult = response as NotFoundObjectResult;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(notFoundObjectResult.StatusCode, Is.EqualTo(404));
                var value = notFoundObjectResult.Value;
                Assert.That(value, Is.Null);
            }
        }

        private static Team CreateTeam(int id, string name)
        {
            return new Team { Id = id, Name = name };
        }

        private static Portfolio CreatePortfolio(int id, string name)
        {
            return new Portfolio { Id = id, Name = name };
        }

        private static Feature CreateFeature(Portfolio portfolio, Team team, int remainingWork)
        {
            var feature = new Feature(team, remainingWork);
            portfolio.Features.Add(feature);

            return feature;
        }

        private TeamController CreateSubject(Team[]? teams = null, Portfolio[]? portfolios = null, Feature[]? features = null)
        {
            teams ??= [];
            portfolios ??= [];
            features ??= [];

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
            portfolioRepositoryMock.Setup(x => x.GetAll()).Returns(portfolios);
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            return new TeamController(
                teamRepositoryMock.Object, portfolioRepositoryMock.Object, featureRepositoryMock.Object, workItemRepoMock.Object, teamUpdateServiceMock.Object);
        }
    }
}
