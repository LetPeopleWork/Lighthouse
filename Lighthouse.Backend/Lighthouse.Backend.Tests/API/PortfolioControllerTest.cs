using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API
{
    public class PortfolioControllerTest
    {
        private Mock<IRepository<Portfolio>> portfolioRepoMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;

        private Mock<IPortfolioUpdater> portfolioUpdaterMock;
        private Mock<IUpdateQueueService> updateQueueServiceMock;
        private Mock<IBlockedItemService> blockedItemServiceMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock;
        private Mock<ITerminologyService> terminologyServiceMock;

        [SetUp]
        public void Setup()
        {
            portfolioRepoMock = new Mock<IRepository<Portfolio>>();
            connectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            terminologyServiceMock = new Mock<ITerminologyService>();
            teamRepoMock = new Mock<IRepository<Team>>();
            rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();
            portfolioUpdaterMock = new Mock<IPortfolioUpdater>();
            updateQueueServiceMock = new Mock<IUpdateQueueService>();
            blockedItemServiceMock = new Mock<IBlockedItemService>();
            blockedItemServiceMock
                .Setup(x => x.GetEffectiveRuleSet(It.IsAny<WorkTrackingSystemOptionsOwner>()))
                .Returns(new WorkItemRuleSet());
            blockedItemServiceMock
                .Setup(x => x.ValidateRuleSet(It.IsAny<WorkItemRuleSet>(), It.IsAny<WorkTrackingSystemOptionsOwner>()))
                .Returns(true);
            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ClaimsPrincipal _, IEnumerable<int> ids, CancellationToken _) => ids.Distinct().ToArray());
        }

        [Test]
        public async Task GetPortfolio_ReturnsSpecificPortfolio()
        {
            var testPortfolios = GetTestPortfolios();
            var testPortfolio = testPortfolios[^1];
            portfolioRepoMock.Setup(x => x.GetById(42)).Returns(testPortfolio);

            var subject = CreateSubject();

            var result = await subject.Get(42);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var portfolioDto = okResult.Value as PortfolioDto;

                Assert.That(portfolioDto.Id, Is.EqualTo(testPortfolio.Id));
                Assert.That(portfolioDto.Name, Is.EqualTo(testPortfolio.Name));
            }
        }

        [Test]
        public async Task GetPortfolio_WhenSomeLinkedTeamsAreUnreadable_FiltersInvolvedTeams()
        {
            var visibleTeam = new Team { Id = 1, Name = "Visible Team" };
            var hiddenTeam = new Team { Id = 2, Name = "Hidden Team" };
            var portfolio = new Portfolio { Id = 42, Name = "Portfolio" };
            portfolio.Features.Add(new Feature(visibleTeam, 3));
            portfolio.Features.Add(new Feature(hiddenTeam, 5));

            portfolioRepoMock.Setup(x => x.GetById(42)).Returns(portfolio);
            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visibleTeam.Id]);

            var subject = CreateSubject();

            var result = await subject.Get(42);

            using (Assert.EnterMultipleScope())
            {
                var okResult = result.Result as OkObjectResult;
                var portfolioDto = okResult!.Value as PortfolioDto;

                Assert.That(portfolioDto!.InvolvedTeams.Select(t => t.Id), Is.EqualTo(new[] { visibleTeam.Id }));
            }
        }

        [Test]
        public async Task GetPortfolio_PortfolioNotFound_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = await subject.Get(1337);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public void UpdateFeaturesForPortfolio_PortfolioExists_UpdatesAndRefreshesForecasts()
        {
            var testPortfolios = GetTestPortfolios();
            var testPortfolio = testPortfolios[^1];
            portfolioRepoMock.Setup(x => x.GetById(42)).Returns(testPortfolio);

            var subject = CreateSubject();

            var result = subject.UpdateFeaturesForPortfolio(42);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkResult>());

                var okResult = result as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                portfolioUpdaterMock.Verify(x => x.TriggerUpdate(testPortfolio.Id));
            }
        }

        [Test]
        public async Task GetPortfolioSettings_PortfolioExists_ReturnsSettings()
        {
            var portfolio = new Portfolio
            {
                Id = 12,
                Name = "El Projecto",
                WorkItemTypes = ["Bug", "Feature"],
                DataRetrievalValue = "SELECT * FROM WorkItems",
                DefaultAmountOfWorkItemsPerFeature = 5,
                WorkTrackingSystemConnectionId = 101
            };

            portfolioRepoMock.Setup(x => x.GetById(12)).Returns(portfolio);

            var subject = CreateSubject();

            var result = await subject.GetPortfolioSettings(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<PortfolioSettingDto>());
                var portfolioSettingDto = okObjectResult.Value as PortfolioSettingDto;

                Assert.That(portfolioSettingDto.Id, Is.EqualTo(portfolio.Id));
                Assert.That(portfolioSettingDto.Name, Is.EqualTo(portfolio.Name));
                Assert.That(portfolioSettingDto.WorkItemTypes, Is.EqualTo(portfolio.WorkItemTypes));
                Assert.That(portfolioSettingDto.DataRetrievalValue, Is.EqualTo(portfolio.DataRetrievalValue));
                Assert.That(portfolioSettingDto.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(portfolio.DefaultAmountOfWorkItemsPerFeature));
                Assert.That(portfolioSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(portfolio.WorkTrackingSystemConnectionId));
            }
        }

        [Test]
        public async Task GetPortfolioSettings_WhenSomeLinkedTeamsAreUnreadable_FiltersInvolvedTeamsAndOwningTeam()
        {
            var visibleTeam = new Team { Id = 1, Name = "Visible Team" };
            var hiddenTeam = new Team { Id = 2, Name = "Hidden Team" };
            var portfolio = new Portfolio { Id = 42, Name = "Portfolio", OwningTeam = hiddenTeam, OwningTeamId = hiddenTeam.Id };
            portfolio.Features.Add(new Feature(visibleTeam, 3));
            portfolio.Features.Add(new Feature(hiddenTeam, 5));

            portfolioRepoMock.Setup(x => x.GetById(42)).Returns(portfolio);
            rbacAdministrationServiceMock
                .Setup(x => x.GetReadableTeamIdsAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([visibleTeam.Id]);

            var subject = CreateSubject();
            var result = await subject.GetPortfolioSettings(42);

            using (Assert.EnterMultipleScope())
            {
                var okResult = result.Result as OkObjectResult;
                var dto = okResult!.Value as PortfolioSettingDto;

                Assert.That(dto!.InvolvedTeams.Select(t => t.Id), Is.EqualTo(new[] { visibleTeam.Id }));
                Assert.That(dto.OwningTeam, Is.Null);
            }
        }


        [Test]
        public async Task GetPortfolioSettings_PortfolioNotFound_ReturnsNotFoundResult()
        {
            var subject = CreateSubject();

            var result = await subject.GetPortfolioSettings(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task UpdatePortfolio_GivenNewPortfolioSettings_UpdatesPortfolioAsync()
        {
            var existingPortfolio = new Portfolio { Id = 132 };
            var existingTeam = new Team { Id = 42, Name = "My Team" };

            portfolioRepoMock.Setup(x => x.GetById(132)).Returns(existingPortfolio);
            teamRepoMock.Setup(x => x.GetById(42)).Returns(existingTeam);

            var updatedPortfolioSettings = new PortfolioSettingDto
            {
                Id = 132,
                Name = "Updated Project",
                WorkItemTypes = ["Feature", "Bug"],
                DataRetrievalValue = "SELECT * FROM UpdatedWorkItems",
                DefaultAmountOfWorkItemsPerFeature = 10,
                WorkTrackingSystemConnectionId = 202,
                SizeEstimateAdditionalFieldDefinitionId = 1,
                OwningTeam = new EntityReferenceDto(existingTeam.Id, existingTeam.Name),
                FeatureOwnerAdditionalFieldDefinitionId = 2,
                ServiceLevelExpectationProbability = 95,
                ServiceLevelExpectationRange = 5,
                SystemWIPLimit = 12,
                BlockedRuleSetJson = "{\"version\":1,\"mode\":\"or\",\"conditions\":[{\"fieldKey\":\"feature.state\",\"operator\":\"equals\",\"value\":\"On Hold\"},{\"fieldKey\":\"feature.tags\",\"operator\":\"contains\",\"value\":\"Waiting for Review\"},{\"fieldKey\":\"feature.tags\",\"operator\":\"contains\",\"value\":\"Customer Feedback\"}]}"
            };

            var subject = CreateSubject();

            var result = await subject.UpdatePortfolio(132, updatedPortfolioSettings);

            portfolioRepoMock.Verify(x => x.Update(existingPortfolio));
            portfolioRepoMock.Verify(x => x.Save());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<PortfolioSettingDto>());
                var portfolioSettingDto = okObjectResult.Value as PortfolioSettingDto;

                Assert.That(portfolioSettingDto.Id, Is.EqualTo(updatedPortfolioSettings.Id));
                Assert.That(portfolioSettingDto.Name, Is.EqualTo(updatedPortfolioSettings.Name));
                Assert.That(portfolioSettingDto.WorkItemTypes, Is.EqualTo(updatedPortfolioSettings.WorkItemTypes));

                Assert.That(portfolioSettingDto.DataRetrievalValue, Is.EqualTo(updatedPortfolioSettings.DataRetrievalValue));
                Assert.That(portfolioSettingDto.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(updatedPortfolioSettings.DefaultAmountOfWorkItemsPerFeature));
                Assert.That(portfolioSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(updatedPortfolioSettings.WorkTrackingSystemConnectionId));
                Assert.That(portfolioSettingDto.SizeEstimateAdditionalFieldDefinitionId, Is.EqualTo(updatedPortfolioSettings.SizeEstimateAdditionalFieldDefinitionId));

                Assert.That(portfolioSettingDto.OwningTeam.Id, Is.EqualTo(existingTeam.Id));
                Assert.That(portfolioSettingDto.OwningTeam.Name, Is.EqualTo(existingTeam.Name));
                Assert.That(portfolioSettingDto.FeatureOwnerAdditionalFieldDefinitionId, Is.EqualTo(updatedPortfolioSettings.FeatureOwnerAdditionalFieldDefinitionId));

                Assert.That(portfolioSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(updatedPortfolioSettings.ServiceLevelExpectationProbability));
                Assert.That(portfolioSettingDto.ServiceLevelExpectationRange, Is.EqualTo(updatedPortfolioSettings.ServiceLevelExpectationRange));

                Assert.That(portfolioSettingDto.SystemWIPLimit, Is.EqualTo(updatedPortfolioSettings.SystemWIPLimit));

                Assert.That(portfolioSettingDto.BlockedRuleSetJson, Does.Contain("On Hold"));
                Assert.That(portfolioSettingDto.BlockedRuleSetJson, Does.Contain("Waiting for Review"));
                Assert.That(portfolioSettingDto.BlockedRuleSetJson, Does.Contain("Customer Feedback"));
            }
        }

        [Test]
        public async Task UpdatePortfolio_BaselineShorterThan14Days_ReturnsBadRequest()
        {
            var updatedPortfolioSettings = new PortfolioSettingDto
            {
                Id = 132,
                Name = "Updated Portfolio",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-10),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(-1),
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.UpdatePortfolio(132, updatedPortfolioSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result.Result as BadRequestObjectResult;
                Assert.That(badRequest.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task UpdatePortfolio_BaselineEndInFuture_ReturnsBadRequest()
        {
            var updatedPortfolioSettings = new PortfolioSettingDto
            {
                Id = 132,
                Name = "Updated Portfolio",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-30),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(5),
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.UpdatePortfolio(132, updatedPortfolioSettings);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result.Result as BadRequestObjectResult;
                Assert.That(badRequest.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task UpdatePortfolio_ValidBaseline_ReturnsOk()
        {
            var existingPortfolio = new Portfolio { Id = 132 };
            var existingTeam = new Team { Id = 42, Name = "My Team" };

            portfolioRepoMock.Setup(x => x.GetById(132)).Returns(existingPortfolio);
            teamRepoMock.Setup(x => x.GetById(42)).Returns(existingTeam);

            var updatedPortfolioSettings = new PortfolioSettingDto
            {
                Id = 132,
                Name = "Updated Portfolio",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-30),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(-1),
                DoneItemsCutoffDays = 180,
                WorkTrackingSystemConnectionId = 1,
                OwningTeam = new EntityReferenceDto(existingTeam.Id, existingTeam.Name),
            };

            var subject = CreateSubject();
            var result = await subject.UpdatePortfolio(132, updatedPortfolioSettings);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public void GetPortfolio_HasPortfolioReadRbacGuardAttribute()
        {
            var method = typeof(PortfolioController).GetMethod(nameof(PortfolioController.Get));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attribute, Is.Not.Null);
                Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.PortfolioRead));
                Assert.That(attribute.ScopeIdRouteKey, Is.EqualTo("portfolioId"));
            }
        }

        [Test]
        public async Task UpdatePortfolio_PortfolioNotFound_ReturnsNotFoundResultAsync()
        {
            var subject = CreateSubject();

            var result = await subject.UpdatePortfolio(1, new PortfolioSettingDto());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = result.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task UpdatePortfolio_BlockedRuleSetFailsValidation_ReturnsBadRequest()
        {
            var portfolio = new Portfolio { Id = 1 };
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(portfolio);
            blockedItemServiceMock
                .Setup(x => x.ValidateRuleSet(It.IsAny<WorkItemRuleSet>(), It.IsAny<WorkTrackingSystemOptionsOwner>()))
                .Returns(false);

            var dto = new PortfolioSettingDto
            {
                WorkTrackingSystemConnectionId = 1,
                BlockedRuleSetJson = "{\"version\":1,\"mode\":\"or\",\"conditions\":[{\"fieldKey\":\"feature.tags\",\"operator\":\"contains\",\"value\":\"\"}]}",
            };

            var subject = CreateSubject();
            var result = await subject.UpdatePortfolio(1, dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdatePortfolio_InvalidStateMappings_ReturnsBadRequest()
        {
            var portfolio = new Portfolio { Id = 1 };
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(portfolio);

            var dto = new PortfolioSettingDto
            {
                WorkTrackingSystemConnectionId = 1,
                StateMappings =
                [
                    new StateMappingDto { Name = "Group A", States = ["Active"] },
                    new StateMappingDto { Name = "Group A", States = ["Resolved"] }
                ]
            };

            var subject = CreateSubject();
            var result = await subject.UpdatePortfolio(1, dto);

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdatePortfolio_NamingAParentOverrideSourceThisVersionDoesNotKnow_IsRefusedNamingTheSourcesItKnows()
        {
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(new Portfolio { Id = 1 });
            var dto = new PortfolioSettingDto { Id = 1, DoneItemsCutoffDays = 180, ParentOverrideSource = (SettingSource)7 };

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            var problem = result.Result.TheProblem();
            using (Assert.EnterMultipleScope())
            {
                Assert.That((result.Result as ObjectResult)?.StatusCode, Is.EqualTo(400));
                Assert.That(problem?.Extensions["code"], Is.EqualTo("portfolio-setting-source-unknown"));
                Assert.That(problem?.Title, Is.EqualTo("The Portfolio names a source this version of Lighthouse does not know."));
                Assert.That(problem?.Detail, Is.EqualTo("The Parent Override Field source must be one of: ConnectionDefault, Own. Nothing was saved."));
            }

            portfolioRepoMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UpdatePortfolio_OwningAParentOverrideFieldItsConnectionDoesNotHave_IsRefused()
        {
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(new Portfolio { Id = 1, WorkTrackingSystemConnectionId = 1, WorkTrackingSystemConnection = AConnectionHoldingCausedBy() });
            var dto = new PortfolioSettingDto { Id = 1, DoneItemsCutoffDays = 180, WorkTrackingSystemConnectionId = 1, ParentOverrideSource = SettingSource.Own, ParentOverrideAdditionalFieldDefinitionId = 9 };

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            var problem = result.Result.TheProblem();
            using (Assert.EnterMultipleScope())
            {
                Assert.That((result.Result as ObjectResult)?.StatusCode, Is.EqualTo(400));
                Assert.That(problem?.Extensions["code"], Is.EqualTo("portfolio-field-not-on-connection"));
                Assert.That(problem?.Title, Is.EqualTo("The Portfolio names a field its connection does not have."));
                Assert.That(problem?.Detail, Is.EqualTo("The Parent Override Field is not an Additional Field of this Portfolio's connection. Nothing was saved."));
            }

            portfolioRepoMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UpdatePortfolio_AFollowersCopyStillNamingAFieldTheConnectionDropped_IsNotRefused()
        {
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(new Portfolio { Id = 1, WorkTrackingSystemConnectionId = 1, WorkTrackingSystemConnection = AConnectionHoldingCausedBy() });
            var dto = new PortfolioSettingDto { Id = 1, DoneItemsCutoffDays = 180, WorkTrackingSystemConnectionId = 1, ParentOverrideSource = SettingSource.ConnectionDefault, ParentOverrideAdditionalFieldDefinitionId = 9 };

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task UpdatePortfolio_NamingAParentOverrideSourceThisVersionDoesNotKnow_IsRefusedInTheWordsThisInstanceUses()
        {
            GivenThisInstanceCalls("Squad", "Squads", "Program", "Programs");
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(new Portfolio { Id = 1 });
            var dto = new PortfolioSettingDto { Id = 1, DoneItemsCutoffDays = 180, ParentOverrideSource = (SettingSource)7 };

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            var problem = result.Result.TheProblem();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem?.Extensions["code"], Is.EqualTo("portfolio-setting-source-unknown"));
                Assert.That(problem?.Title, Is.EqualTo("The Program names a source this version of Lighthouse does not know."));
            }
        }

        [Test]
        public async Task UpdatePortfolio_OwningAParentOverrideFieldItsConnectionDoesNotHave_IsRefusedInTheWordsThisInstanceUses()
        {
            GivenThisInstanceCalls("Squad", "Squads", "Program", "Programs");
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(new Portfolio { Id = 1, WorkTrackingSystemConnectionId = 1, WorkTrackingSystemConnection = AConnectionHoldingCausedBy() });
            var dto = new PortfolioSettingDto { Id = 1, DoneItemsCutoffDays = 180, WorkTrackingSystemConnectionId = 1, ParentOverrideSource = SettingSource.Own, ParentOverrideAdditionalFieldDefinitionId = 9 };

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            var problem = result.Result.TheProblem();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem?.Title, Is.EqualTo("The Program names a field its connection does not have."));
                Assert.That(problem?.Detail, Is.EqualTo("The Parent Override Field is not an Additional Field of this Program's connection. Nothing was saved."));
            }
        }

        private void GivenThisInstanceCalls(string team, string teams, string portfolio, string portfolios)
            => terminologyServiceMock.Setup(x => x.GetAll()).Returns(
            [
                new TerminologyEntry { Id = 1, Key = "team", DefaultValue = "Team", Value = team },
                new TerminologyEntry { Id = 2, Key = "teams", DefaultValue = "Teams", Value = teams },
                new TerminologyEntry { Id = 3, Key = "portfolio", DefaultValue = "Portfolio", Value = portfolio },
                new TerminologyEntry { Id = 4, Key = "portfolios", DefaultValue = "Portfolios", Value = portfolios },
            ]);

        [Test]
        public async Task UpdatePortfolio_MovingToAnotherConnectionWhileOwningAFieldOfTheOldOne_IsRefused()
        {
            var dto = GivenAPortfolioOwningCausedByMovesToAConnectionHoldingParentLink();
            dto.ParentOverrideAdditionalFieldDefinitionId = 5;

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((result.Result as ObjectResult)?.StatusCode, Is.EqualTo(400));
                Assert.That(result.Result.TheProblem()?.Extensions["code"], Is.EqualTo("portfolio-field-not-on-connection"));
            }

            portfolioRepoMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UpdatePortfolio_MovingToAnotherConnectionWhileOwningAFieldTheNewOneHas_IsAccepted()
        {
            var dto = GivenAPortfolioOwningCausedByMovesToAConnectionHoldingParentLink();
            dto.ParentOverrideAdditionalFieldDefinitionId = 7;

            var result = await CreateSubject().AnsweringWithProblemDetails().UpdatePortfolio(1, dto);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        private PortfolioSettingDto GivenAPortfolioOwningCausedByMovesToAConnectionHoldingParentLink()
        {
            var target = new WorkTrackingSystemConnection { Id = 2, Name = "Jira Treasury" };
            target.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 7, DisplayName = "Parent Link", Reference = "customfield_10014" });
            connectionRepositoryMock.Setup(x => x.GetById(2)).Returns(target);
            portfolioRepoMock.Setup(x => x.GetById(1)).Returns(new Portfolio { Id = 1, WorkTrackingSystemConnectionId = 1, WorkTrackingSystemConnection = AConnectionHoldingCausedBy() });
            return new PortfolioSettingDto { Id = 1, DoneItemsCutoffDays = 180, WorkTrackingSystemConnectionId = 2, ParentOverrideSource = SettingSource.Own };
        }

        private static WorkTrackingSystemConnection AConnectionHoldingCausedBy()
        {
            var connection = new WorkTrackingSystemConnection { Id = 1, Name = "Jira" };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 5, DisplayName = "Caused by", Reference = "customfield_10500" });
            return connection;
        }

        private PortfolioController CreateSubject()
        {
            return new PortfolioController(
                portfolioRepoMock.Object,
                teamRepoMock.Object,
                portfolioUpdaterMock.Object,
                rbacAdministrationServiceMock.Object,
                FeatureOrderingTestHelper.FollowingTheTracker(),
                blockedItemServiceMock.Object,
                updateQueueServiceMock.Object,
                TestToday.Clock,
                new ParentOverrideRefusals(connectionRepositoryMock.Object, terminologyServiceMock.Object)
            );
        }

        private static List<Portfolio> GetTestPortfolios()
        {
            return
            [
                new Portfolio { Id = 12, Name = "Foo" },
                new Portfolio { Id = 42, Name = "Bar" }
            ];
        }

    }
}
