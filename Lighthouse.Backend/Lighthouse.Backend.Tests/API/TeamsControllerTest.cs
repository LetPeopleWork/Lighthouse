using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class TeamsControllerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionRepositoryMock;

        private Mock<ITeamUpdater> teamUpdateServiceMock;
        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;
        private Mock<IForecastFilterRuleService> forecastFilterRuleServiceMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            workTrackingSystemConnectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            licenseServiceMock = new Mock<ILicenseService>();
            teamUpdateServiceMock = new Mock<ITeamUpdater>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock.Setup(x => x.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);
            rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();
            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IEnumerable<int> ids, CancellationToken _) => ids.Distinct().ToArray());
            forecastFilterRuleServiceMock = new Mock<IForecastFilterRuleService>();
        }

        [Test]
        public async Task GetTeams_SingleTeam_NoPortfolios()
        {
            var team = CreateTeam(1, "Numero Uno");

            var subject = CreateSubject([team]);

            var results = (await subject.GetTeams()).ToList();

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
        public async Task GetTeams_SingleTeam_SinglePortfolio_SingleFeature()
        {
            var team = CreateTeam(1, "Numero Uno");
            var portfolio = CreatePortfolio(42, "My Portfolio");

            CreateFeature(portfolio, team, 12);

            var subject = CreateSubject([team], [portfolio]);

            var results = (await subject.GetTeams()).ToList();

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
        public async Task GetTeams_SingleTeam_SinglePortfolio_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var portfolio = CreatePortfolio(42, "My Portfolio");

            CreateFeature(portfolio, team, 12);
            CreateFeature(portfolio, team, 42);

            var subject = CreateSubject([team], [portfolio]);

            var results = (await subject.GetTeams()).ToList();

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
        public async Task GetTeams_SingleTeam_MultiplePortfolios_MultipleFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var portfolio1 = CreatePortfolio(42, "My Portfolio");

            CreateFeature(portfolio1, team, 12);
            CreateFeature(portfolio1, team, 42);

            var portfolio2 = CreatePortfolio(13, "My Other Portfolio");

            CreateFeature(portfolio2, team, 5);

            var subject = CreateSubject([team], [portfolio1, portfolio2]);

            var results = (await subject.GetTeams()).ToList();

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
        public async Task GetTeams_WhenLinkedPortfoliosAreUnreadable_FiltersPortfoliosAndFeatures()
        {
            var team = CreateTeam(1, "Numero Uno");
            var visiblePortfolio = CreatePortfolio(42, "Visible Portfolio");
            var hiddenPortfolio = CreatePortfolio(13, "Hidden Portfolio");

            CreateFeature(visiblePortfolio, team, 12);
            CreateFeature(hiddenPortfolio, team, 5);

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadablePortfolioIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visiblePortfolio.Id]);

            var subject = CreateSubject([team], [visiblePortfolio, hiddenPortfolio]);

            var result = (await subject.GetTeams()).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Portfolios.Select(p => p.Id), Is.EqualTo(new[] { visiblePortfolio.Id }));
                Assert.That(result.Features, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task GetTeams_MultipleTeams_MultiplePortfolios_MultipleFeatures()
        {
            var team1 = CreateTeam(1, "Numero Uno");
            var portfolio1 = CreatePortfolio(42, "My Portfolio");

            CreateFeature(portfolio1, team1, 12);
            CreateFeature(portfolio1, team1, 42);

            var team2 = CreateTeam(2, "Una Mas");
            var portfolio2 = CreatePortfolio(13, "My Other Portfolio");

            CreateFeature(portfolio2, team2, 5);

            var subject = CreateSubject([team1, team2], [portfolio1, portfolio2]);

            var results = (await subject.GetTeams()).ToList();

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
                BlockedRuleSetJson = "{\"version\":1,\"mode\":\"or\",\"conditions\":[{\"fieldKey\":\"workitem.state\",\"operator\":\"equals\",\"value\":\"Blocked\"},{\"fieldKey\":\"workitem.tags\",\"operator\":\"contains\",\"value\":\"Waiting\"},{\"fieldKey\":\"workitem.tags\",\"operator\":\"contains\",\"value\":\"Customer Input Needed\"}]}",
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

                Assert.That(teamSettingDto.BlockedRuleSetJson, Does.Contain("Blocked"));
                Assert.That(teamSettingDto.BlockedRuleSetJson, Does.Contain("Waiting"));
                Assert.That(teamSettingDto.BlockedRuleSetJson, Does.Contain("Customer Input Needed"));
            }
        }

        [Test]
        public async Task CreateTeam_BaselineShorterThan14Days_ReturnsBadRequest()
        {
            var newTeamSettings = new TeamSettingDto
            {
                Name = "New Team",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-10),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(-1),
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.CreateTeam(newTeamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result.Result as BadRequestObjectResult;
                Assert.That(badRequest.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreateTeam_BaselineEndInFuture_ReturnsBadRequest()
        {
            var newTeamSettings = new TeamSettingDto
            {
                Name = "New Team",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-30),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(5),
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.CreateTeam(newTeamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result.Result as BadRequestObjectResult;
                Assert.That(badRequest.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreateTeam_ValidBaseline_ReturnsOk()
        {
            var newTeamSettings = new TeamSettingDto
            {
                Name = "New Team",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-30),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(-1),
                DoneItemsCutoffDays = 180,
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.CreateTeam(newTeamSettings);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
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
        public async Task ValidateTeamSettings_GivenValidTeamSettings_ReturnsOkResultFromWorkItemService()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1886, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            var teamSettings = new TeamSettingDto { WorkTrackingSystemConnectionId = 1886 };
            var expectedResult = ConnectionValidationResult.Success();

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

                var value = okObjectResult.Value as ConnectionValidationResult;
                Assert.That(value, Is.Not.Null);
                Assert.That(value!.IsValid, Is.True);
            }
        }

        [Test]
        public async Task ValidateTeamSettings_GivenInvalidTeamSettings_ReturnsBadRequestResultFromWorkItemService()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1886, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            var teamSettings = new TeamSettingDto { WorkTrackingSystemConnectionId = 1886 };
            var expectedResult = ConnectionValidationResult.Failure("no_work_items_found", "No work items found.", "Check query.");

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingSystemConnectionRepositoryMock.Setup(x => x.GetById(1886)).Returns(workTrackingSystemConnection);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystemConnection.WorkTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);
            workTrackingConnectorServiceMock.Setup(x => x.ValidateTeamSettings(It.IsAny<Team>())).ReturnsAsync(expectedResult);

            var subject = CreateSubject();

            var response = await subject.ValidateTeamSettings(teamSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<BadRequestObjectResult>());

                var badRequestObjectResult = response.Result as BadRequestObjectResult;
                Assert.That(badRequestObjectResult!.StatusCode, Is.EqualTo(400));

                var value = badRequestObjectResult.Value as ConnectionValidationResult;
                Assert.That(value, Is.Not.Null);
                Assert.That(value!.IsValid, Is.False);
                Assert.That(value.Code, Is.EqualTo("no_work_items_found"));
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

        [Test]
        public async Task GetTeams_WithBlackoutOverlap_SetsHasThroughputBlackoutOverlapTrue()
        {
            var team = CreateTeam(1, "Team 1");
            team.ThroughputHistory = 30;

            var throughputSettings = team.GetThroughputSettings();
            var midPoint = throughputSettings.StartDate.AddDays(10);

            blackoutPeriodServiceMock.Setup(x => x.GetEffectiveBlackoutDays(throughputSettings.StartDate, throughputSettings.EndDate)).Returns([
                new BlackoutPeriod { Start = DateOnly.FromDateTime(midPoint), End = DateOnly.FromDateTime(midPoint.AddDays(2)) }
            ]);

            var subject = CreateSubject([team]);
            var results = (await subject.GetTeams()).ToList();

            Assert.That(results.Single().HasThroughputBlackoutOverlap, Is.True);
        }

        [Test]
        public async Task GetTeams_WithoutBlackoutOverlap_SetsHasThroughputBlackoutOverlapFalse()
        {
            var team = CreateTeam(1, "Team 1");
            team.ThroughputHistory = 30;

            blackoutPeriodServiceMock.Setup(x => x.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([
                new BlackoutPeriod { Start = new DateOnly(2020, 1, 1), End = new DateOnly(2020, 1, 5) }
            ]);

            var subject = CreateSubject([team]);
            var results = (await subject.GetTeams()).ToList();

            Assert.That(results.Single().HasThroughputBlackoutOverlap, Is.False);
        }

        [Test]
        public async Task GetTeams_WithEffectiveForecastFilter_SetsHasForecastFilterTrue()
        {
            var team = CreateTeam(1, "Team 1");
            forecastFilterRuleServiceMock
                .Setup(x => x.GetEffectiveRuleSet(team))
                .Returns(new WorkItemRuleSet());

            var subject = CreateSubject([team]);
            var results = (await subject.GetTeams()).ToList();

            Assert.That(results.Single().HasForecastFilter, Is.True);
        }

        [Test]
        public async Task GetTeams_WithoutEffectiveForecastFilter_SetsHasForecastFilterFalse()
        {
            var team = CreateTeam(1, "Team 1");
            forecastFilterRuleServiceMock
                .Setup(x => x.GetEffectiveRuleSet(team))
                .Returns((WorkItemRuleSet?)null);

            var subject = CreateSubject([team]);
            var results = (await subject.GetTeams()).ToList();

            Assert.That(results.Single().HasForecastFilter, Is.False);
        }

        [Test]
        public async Task GetTeams_WhenRbacIsEnabled_FiltersToReadableTeams()
        {
            var firstTeam = CreateTeam(1, "Visible Team");
            var secondTeam = CreateTeam(2, "Hidden Team");

            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([firstTeam.Id]);

            var subject = CreateSubject([firstTeam, secondTeam]);

            var results = (await subject.GetTeams()).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].Id, Is.EqualTo(firstTeam.Id));
                Assert.That(results[0].Name, Is.EqualTo(firstTeam.Name));
            }
        }

        [Test]
        public void UpdateAllTeams_HasSystemAdminRbacGuardAttribute()
        {
            var method = typeof(TeamsController).GetMethod(nameof(TeamsController.UpdateAllTeams));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin));
        }

        private static Team CreateTeam(int id, string name)
        {
            return new Team { Id = id, Name = name };
        }

        [Test]
        public async Task CreateTeam_InvalidStateMappings_ReturnsBadRequest()
        {
            var dto = new TeamSettingDto
            {
                WorkTrackingSystemConnectionId = 1,
                StateMappings =
                [
                    new StateMappingDto { Name = "", States = ["Active"] }
                ]
            };

            var subject = CreateSubject();
            var result = await subject.CreateTeam(dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        private static Portfolio CreatePortfolio(int id, string name)
        {
            return new Portfolio { Id = id, Name = name };
        }

        private static void CreateFeature(Portfolio portfolio, Team team, int remainingWork)
        {
            var feature = new Feature(team, remainingWork);
            portfolio.Features.Add(feature);
        }

        private TeamsController CreateSubject(Team[]? teams = null, Portfolio[]? portfolios = null)
        {
            teams ??= [];
            portfolios ??= [];

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(teams);
            portfolioRepositoryMock.Setup(x => x.GetAll()).Returns(portfolios);

            return new TeamsController(
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                workTrackingSystemConnectionRepositoryMock.Object,
                teamUpdateServiceMock.Object,
                workTrackingConnectorFactoryMock.Object,
                licenseServiceMock.Object,
                blackoutPeriodServiceMock.Object,
                rbacAdministrationServiceMock.Object,
                forecastFilterRuleServiceMock.Object);
        }
    }
}
