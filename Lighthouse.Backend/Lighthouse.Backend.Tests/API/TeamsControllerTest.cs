using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class TeamsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionRepositoryMock;

        private Mock<ITeamUpdater> teamUpdateServiceMock;
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            workTrackingSystemConnectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            licenseServiceMock = new Mock<ILicenseService>();
            teamUpdateServiceMock = new Mock<ITeamUpdater>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
        }

        [Test]
        public void GetTeams_SingleTeam_NoPortfolios()
        {
            var team = CreateTeam(1, "Numero Uno");

            var subject = CreateSubject([team]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Portfolios, Has.Count.EqualTo(0));
                Assert.That(result.Features, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void GetTeams_SingleTeam_SinglePortfolio_SingleFeature()
        {
            var team = CreateTeam(1, "Numero Uno");
            var portfolio = CreatePortfolio(42, "My Portfolio");
            portfolio.UpdateTeams([team]);

            var feature = CreateFeature(portfolio, team, 12);

            var subject = CreateSubject([team], [portfolio], [feature]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Portfolios, Has.Count.EqualTo(1));
                Assert.That(result.Features, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void GetTeams_SingleTeam_SinglePortfolio_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var portfolio = CreatePortfolio(42, "My Portfolio");
            portfolio.UpdateTeams([team]);

            var feature1 = CreateFeature(portfolio, team, 12);
            var feature2 = CreateFeature(portfolio, team, 42);

            var subject = CreateSubject([team], [portfolio], [feature1, feature2]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Portfolios, Has.Count.EqualTo(1));
                Assert.That(result.Features, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void GetTeams_SingleTeam_MultiplePortfolios_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var portfolio1 = CreatePortfolio(42, "My Portfolio");
            portfolio1.UpdateTeams([team]);

            var feature1 = CreateFeature(portfolio1, team, 12);
            var feature2 = CreateFeature(portfolio1, team, 42);

            var portfolio2 = CreatePortfolio(13, "My Other Portfolio");
            portfolio2.UpdateTeams([team]);

            var feature3 = CreateFeature(portfolio2, team, 5);

            var subject = CreateSubject([team], [portfolio1, portfolio2], [feature1, feature2, feature3]);

            var results = subject.GetTeams().ToList();

            var result = results.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Numero Uno"));
                Assert.That(result.Portfolios, Has.Count.EqualTo(2));
                Assert.That(result.Features, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public void GetTeams_MultipleTeams_MultiplePortfolios_MultipleFeatures()
        {
            var team1 = CreateTeam(1, "Numero Uno");
            var portfolio1 = CreatePortfolio(42, "My Portfolio");
            portfolio1.UpdateTeams([team1]);

            var feature1 = CreateFeature(portfolio1, team1, 12);
            var feature2 = CreateFeature(portfolio1, team1, 42);

            var team2 = CreateTeam(2, "Una Mas");
            var portfolio2 = CreatePortfolio(13, "My Other Portfolio");
            portfolio2.UpdateTeams([team2]);

            var feature3 = CreateFeature(portfolio2, team2, 5);

            var subject = CreateSubject([team1, team2], [portfolio1, portfolio2], [feature1, feature2, feature3]);

            var results = subject.GetTeams().ToList();

            var team1Results = results[0];
            var team2Results = results[^1];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(team1Results.Id, Is.EqualTo(1));
                Assert.That(team2Results.Id, Is.EqualTo(2));

                Assert.That(team1Results.Name, Is.EqualTo("Numero Uno"));
                Assert.That(team2Results.Name, Is.EqualTo("Una Mas"));

                Assert.That(team1Results.Portfolios, Has.Count.EqualTo(1));
                Assert.That(team2Results.Portfolios, Has.Count.EqualTo(1));

                Assert.That(team1Results.Features, Has.Count.EqualTo(2));
                Assert.That(team2Results.Features, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task CreateTeam_GivenNewTeamSettings_CreatesTeamAsync()
        {
            var newTeamSettings = new TeamSettingDto
            {
                Name = "New Team",
                FeatureWIP = 12,
                ThroughputHistory = 30,
                DataRetrievalValue = "project = MyProject",
                WorkItemTypes = ["User Story", "Bug"],
                WorkTrackingSystemConnectionId = 2,
                ParentOverrideAdditionalFieldDefinitionId = 7,
                ToDoStates = [" To Do"],
                DoingStates = ["Doing"],
                DoneStates = ["Done "],
                ServiceLevelExpectationProbability = 50,
                ServiceLevelExpectationRange = 2,
                SystemWIPLimit = 3,
                BlockedStates = ["Blocked"],
                BlockedTags = ["Waiting", "Customer Input Needed"],
            };

            var subject = CreateSubject();

            var result = await subject.CreateTeam(newTeamSettings);

            teamRepositoryMock.Verify(x => x.Add(It.IsAny<Team>()));
            teamRepositoryMock.Verify(x => x.Save());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<TeamSettingDto>());
                var teamSettingDto = okObjectResult.Value as TeamSettingDto;

                Assert.That(teamSettingDto.Name, Is.EqualTo(newTeamSettings.Name));
                Assert.That(teamSettingDto.ThroughputHistory, Is.EqualTo(newTeamSettings.ThroughputHistory));
                Assert.That(teamSettingDto.FeatureWIP, Is.EqualTo(newTeamSettings.FeatureWIP));
                Assert.That(teamSettingDto.DataRetrievalValue, Is.EqualTo(newTeamSettings.DataRetrievalValue));
                Assert.That(teamSettingDto.WorkItemTypes, Is.EqualTo(newTeamSettings.WorkItemTypes));
                Assert.That(teamSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(newTeamSettings.WorkTrackingSystemConnectionId));
                Assert.That(teamSettingDto.ParentOverrideAdditionalFieldDefinitionId, Is.EqualTo(newTeamSettings.ParentOverrideAdditionalFieldDefinitionId));

                Assert.That(teamSettingDto.ToDoStates, Contains.Item("To Do"));
                Assert.That(teamSettingDto.DoingStates, Contains.Item("Doing"));
                Assert.That(teamSettingDto.DoneStates, Contains.Item("Done"));

                Assert.That(teamSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(newTeamSettings.ServiceLevelExpectationProbability));
                Assert.That(teamSettingDto.ServiceLevelExpectationRange, Is.EqualTo(newTeamSettings.ServiceLevelExpectationRange));

                Assert.That(teamSettingDto.SystemWIPLimit, Is.EqualTo(newTeamSettings.SystemWIPLimit));

                Assert.That(teamSettingDto.BlockedStates, Contains.Item("Blocked"));
                Assert.That(teamSettingDto.BlockedTags, Contains.Item("Waiting"));
                Assert.That(teamSettingDto.BlockedTags, Contains.Item("Customer Input Needed"));
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task CreateTeam_GivenExistingTeamWithCSVWorkTrackingConnector_CanOnlyAddWithPremiumLicense(bool hasPremium)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(hasPremium);

            var csvWorkTrackingConnection = new WorkTrackingSystemConnection
            {
                Id = 1,
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };

            workTrackingSystemConnectionRepositoryMock.Setup(x => x.GetAll()).Returns([csvWorkTrackingConnection]);

            var existingTeam = new Team
            {
                Id = 1,
                Name = "CSV",
                WorkTrackingSystemConnection = csvWorkTrackingConnection
            };

            var newTeamSettings = new TeamSettingDto
            {
                Name = "New Team",
                WorkTrackingSystemConnectionId = 1,
            };

            var subject = CreateSubject([existingTeam]);

            var response = await subject.CreateTeam(newTeamSettings);

            var expectedResponseType = hasPremium ? typeof(OkObjectResult) : typeof(ObjectResult);
            var expectedStatusCode = hasPremium ? 200 : 403;
            var expectedTimes = hasPremium ? Times.Once() : Times.Never();

            teamRepositoryMock.Verify(x => x.Add(It.IsAny<Team>()), expectedTimes);
            teamRepositoryMock.Verify(x => x.Save(), expectedTimes);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf(expectedResponseType));

                var result = (ObjectResult)response.Result;
                Assert.That(result.StatusCode, Is.EqualTo(expectedStatusCode));
            }
        }

        [Test]
        public void UpdateAllTeamData_TriggersUpdateOfAllTeams()
        {
            var expectedTeam = new Team { Id = 12 };
            teamRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(expectedTeam);

            var subject = CreateSubject([expectedTeam, expectedTeam, expectedTeam]);

            var response = subject.UpdateAllTeams();

            teamUpdateServiceMock.Verify(x => x.TriggerUpdate(expectedTeam.Id), Times.Exactly(3));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());
                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ValidateTeamSettings_GivenTeamSettings_ReturnsResultFromWorkItemService(bool expectedResult)
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1886, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            var teamSettings = new TeamSettingDto { WorkTrackingSystemConnectionId = 1886 };

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingSystemConnectionRepositoryMock.Setup(x => x.GetById(1886)).Returns(workTrackingSystemConnection);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystemConnection.WorkTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);
            workTrackingConnectorServiceMock.Setup(x => x.ValidateTeamSettings(It.IsAny<Team>())).ReturnsAsync(expectedResult);

            var subject = CreateSubject();

            var response = await subject.ValidateTeamSettings(teamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = response.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                var value = okObjectResult.Value;
                Assert.That(value, Is.EqualTo(expectedResult));
            }
        }

        [Test]
        public async Task ValidateTeamSettings_WorkTrackingSystemNotFound_ReturnsNotFound()
        {
            var teamSettings = new TeamSettingDto { WorkTrackingSystemConnectionId = 1886 };

            workTrackingSystemConnectionRepositoryMock.Setup(x => x.GetById(1886)).Returns((WorkTrackingSystemConnection)null);

            var subject = CreateSubject();

            var response = await subject.ValidateTeamSettings(teamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundObjectResult = response.Result as NotFoundResult;
                Assert.That(notFoundObjectResult.StatusCode, Is.EqualTo(404));
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

        private static Feature CreateFeature(Portfolio project, Team team, int remainingWork)
        {
            var feature = new Feature(team, remainingWork);
            project.Features.Add(feature);

            return feature;
        }

        private TeamsController CreateSubject(Team[]? teams = null, Portfolio[]? portfolios = null, Feature[]? features = null)
        {
            teams ??= [];
            portfolios ??= [];
            features ??= [];

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
            portfolioRepositoryMock.Setup(x => x.GetAll()).Returns(portfolios);
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            return new TeamsController(
                teamRepositoryMock.Object, portfolioRepositoryMock.Object, featureRepositoryMock.Object, workTrackingSystemConnectionRepositoryMock.Object, teamUpdateServiceMock.Object, workTrackingConnectorFactoryMock.Object, licenseServiceMock.Object);
        }
    }
}
