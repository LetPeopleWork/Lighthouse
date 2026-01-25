using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class PortfolioControllerTest
    {
        private Mock<IRepository<Portfolio>> portfolioRepoMock;
        private Mock<IRepository<Team>> teamRepoMock;

        private Mock<IPortfolioUpdater> portfolioUpdaterMock;

        [SetUp]
        public void Setup()
        {
            portfolioRepoMock = new Mock<IRepository<Portfolio>>();
            teamRepoMock = new Mock<IRepository<Team>>();
            portfolioUpdaterMock = new Mock<IPortfolioUpdater>();
        }

        [Test]
        public void GetPortfolio_ReturnsSpecificPortfolio()
        {
            var testPortfolios = GetTestPortfolios();
            var testPortfolio = testPortfolios[^1];
            portfolioRepoMock.Setup(x => x.GetById(42)).Returns(testPortfolio);

            var subject = CreateSubject();

            var result = subject.Get(42);

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
        public void GetPortfolio_PortfolioNotFound_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.Get(1337);

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
        public async Task Delete_RemovesTeamAndSaves()
        {
            const int portfolioId = 12;

            var subject = CreateSubject();

            await subject.DeletePortfolio(portfolioId);

            portfolioRepoMock.Verify(x => x.Remove(portfolioId));
            portfolioRepoMock.Verify(x => x.Save());
        }

        [Test]
        public void GetPortfolioSettings_PortfolioExists_ReturnsSettings()
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

            var result = subject.GetPortfolioSettings(12);

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
        public void GetPortfolioSettings_PortfoliootFound_ReturnsNotFoundResult()
        {
            var subject = CreateSubject();

            var result = subject.GetPortfolioSettings(1);

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
                InvolvedTeams = [new EntityReferenceDto(existingTeam.Id, existingTeam.Name)],
                OwningTeam = new EntityReferenceDto(existingTeam.Id, existingTeam.Name),
                FeatureOwnerAdditionalFieldDefinitionId = 2,
                ServiceLevelExpectationProbability = 95,
                ServiceLevelExpectationRange = 5,
                SystemWIPLimit = 12,
                BlockedStates = ["On Hold"],
                BlockedTags = ["Waiting for Review", "Customer Feedback"]
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

                Assert.That(portfolioSettingDto.InvolvedTeams, Has.Count.EqualTo(1));
                var teamDto = portfolioSettingDto.InvolvedTeams.Single();
                Assert.That(teamDto.Id, Is.EqualTo(existingTeam.Id));
                Assert.That(teamDto.Name, Is.EqualTo(existingTeam.Name));

                Assert.That(portfolioSettingDto.OwningTeam.Id, Is.EqualTo(existingTeam.Id));
                Assert.That(portfolioSettingDto.OwningTeam.Name, Is.EqualTo(existingTeam.Name));
                Assert.That(portfolioSettingDto.FeatureOwnerAdditionalFieldDefinitionId, Is.EqualTo(updatedPortfolioSettings.FeatureOwnerAdditionalFieldDefinitionId));

                Assert.That(portfolioSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(updatedPortfolioSettings.ServiceLevelExpectationProbability));
                Assert.That(portfolioSettingDto.ServiceLevelExpectationRange, Is.EqualTo(updatedPortfolioSettings.ServiceLevelExpectationRange));

                Assert.That(portfolioSettingDto.SystemWIPLimit, Is.EqualTo(updatedPortfolioSettings.SystemWIPLimit));

                Assert.That(portfolioSettingDto.BlockedStates, Contains.Item("On Hold"));
                Assert.That(portfolioSettingDto.BlockedTags, Contains.Item("Waiting for Review"));
                Assert.That(portfolioSettingDto.BlockedTags, Contains.Item("Customer Feedback"));
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

        private PortfolioController CreateSubject()
        {
            return new PortfolioController(
                portfolioRepoMock.Object,
                teamRepoMock.Object,
                portfolioUpdaterMock.Object
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
