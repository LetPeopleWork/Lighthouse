using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class PortfoliosControllerTest
    {
        private Mock<IRepository<Portfolio>> portfolioRepoMock;
        private Mock<IRepository<Team>> teamRepoMock;

        private Mock<IPortfolioUpdater> portfolioUpdaterMock;

        private Mock<IWorkTrackingConnectorFactory> workTrackingConnectorFactoryMock;

        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionRepoMock;

        [SetUp]
        public void Setup()
        {
            portfolioRepoMock = new Mock<IRepository<Portfolio>>();
            teamRepoMock = new Mock<IRepository<Team>>();
            portfolioUpdaterMock = new Mock<IPortfolioUpdater>();
            workTrackingConnectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            workTrackingSystemConnectionRepoMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
        }

        [Test]
        public void GetPortfolios_ReturnsAllPortfoliosFromRepository()
        {
            var testPortfolios = GetTestPortfolios();
            portfolioRepoMock.Setup(x => x.GetAll()).Returns(testPortfolios);

            var subject = CreateSubject();

            var result = subject.GetPortfolios().ToList();

            Assert.That(result, Has.Count.EqualTo(testPortfolios.Count));
        }

        [Test]
        public void UpdateAllPortfolioData_TriggersUpdateOfAllPortfolios()
        {
            var testPortfolios = GetTestPortfolios();

            portfolioRepoMock.Setup(x => x.GetAll()).Returns(testPortfolios);
            foreach (var portfolio in testPortfolios)
            {
                portfolioRepoMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            }

            var subject = CreateSubject();

            var response = subject.UpdateAllPortfolios();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());
                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                foreach (var portfolio in testPortfolios)
                {
                    portfolioUpdaterMock.Verify(x => x.TriggerUpdate(portfolio.Id), Times.Once);
                }
            }
        }

        [Test]
        public async Task CreatePortfolio_GivenNewPortfolioSettings_CreatesPortfolioAsync()
        {
            var newPortfolioSetting = new PortfolioSettingDto
            {
                Name = "New Portfolio",
                WorkItemTypes = ["Bug", "Feature"],
                DataRetrievalValue = "SELECT * FROM WorkItems",
                DefaultAmountOfWorkItemsPerFeature = 5,
                WorkTrackingSystemConnectionId = 101,
                ToDoStates = ["To Do "],
                DoingStates = [" In Progress"],
                DoneStates = ["Done"],
                ServiceLevelExpectationProbability = 90,
                ServiceLevelExpectationRange = 10,
                SystemWIPLimit = 7,
                BlockedStates = ["Blocked"],
                BlockedTags = ["Waiting", "Customer Input"],
            };

            var subject = CreateSubject();

            var result = await subject.CreatePortfolio(newPortfolioSetting);

            portfolioRepoMock.Verify(x => x.Add(It.IsAny<Portfolio>()));
            portfolioRepoMock.Verify(x => x.Save());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okObjectResult = result.Result as OkObjectResult;
                Assert.That(okObjectResult.StatusCode, Is.EqualTo(200));

                Assert.That(okObjectResult.Value, Is.InstanceOf<PortfolioSettingDto>());
                var portfolioSettingDto = okObjectResult.Value as PortfolioSettingDto;

                Assert.That(portfolioSettingDto.Name, Is.EqualTo(newPortfolioSetting.Name));
                Assert.That(portfolioSettingDto.WorkItemTypes, Is.EqualTo(newPortfolioSetting.WorkItemTypes));

                Assert.That(portfolioSettingDto.DataRetrievalValue, Is.EqualTo(newPortfolioSetting.DataRetrievalValue));
                Assert.That(portfolioSettingDto.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(newPortfolioSetting.DefaultAmountOfWorkItemsPerFeature));
                Assert.That(portfolioSettingDto.WorkTrackingSystemConnectionId, Is.EqualTo(newPortfolioSetting.WorkTrackingSystemConnectionId));

                Assert.That(portfolioSettingDto.ToDoStates, Contains.Item("To Do"));
                Assert.That(portfolioSettingDto.DoingStates, Contains.Item("In Progress"));
                Assert.That(portfolioSettingDto.DoneStates, Contains.Item("Done"));

                Assert.That(portfolioSettingDto.ServiceLevelExpectationProbability, Is.EqualTo(newPortfolioSetting.ServiceLevelExpectationProbability));
                Assert.That(portfolioSettingDto.ServiceLevelExpectationRange, Is.EqualTo(newPortfolioSetting.ServiceLevelExpectationRange));

                Assert.That(portfolioSettingDto.SystemWIPLimit, Is.EqualTo(newPortfolioSetting.SystemWIPLimit));

                Assert.That(portfolioSettingDto.BlockedStates, Contains.Item("Blocked"));
                Assert.That(portfolioSettingDto.BlockedTags, Contains.Item("Waiting"));
                Assert.That(portfolioSettingDto.BlockedTags, Contains.Item("Customer Input"));
            }
        }

        [Test]
        public async Task CreatePortfolio_BaselineShorterThan14Days_ReturnsBadRequest()
        {
            var newPortfolioSetting = new PortfolioSettingDto
            {
                Name = "New Portfolio",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-10),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(-1),
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.CreatePortfolio(newPortfolioSetting);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result.Result as BadRequestObjectResult;
                Assert.That(badRequest.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreatePortfolio_BaselineEndInFuture_ReturnsBadRequest()
        {
            var newPortfolioSetting = new PortfolioSettingDto
            {
                Name = "New Portfolio",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-30),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(5),
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.CreatePortfolio(newPortfolioSetting);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
                var badRequest = result.Result as BadRequestObjectResult;
                Assert.That(badRequest.StatusCode, Is.EqualTo(400));
            }
        }

        [Test]
        public async Task CreatePortfolio_ValidBaseline_ReturnsOk()
        {
            var newPortfolioSetting = new PortfolioSettingDto
            {
                Name = "New Portfolio",
                ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.Date.AddDays(-30),
                ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.Date.AddDays(-1),
                DoneItemsCutoffDays = 180,
                WorkTrackingSystemConnectionId = 1
            };

            var subject = CreateSubject();
            var result = await subject.CreatePortfolio(newPortfolioSetting);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ValidatePortfolioSettings_GivenPortfolioSettings_ReturnsResultFromWorkItemService(bool expectedResult)
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 1886, WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            var portfolioSettingDto = new PortfolioSettingDto { WorkTrackingSystemConnectionId = 1886 };

            var workTrackingConnectorServiceMock = new Mock<IWorkTrackingConnector>();
            workTrackingSystemConnectionRepoMock.Setup(x => x.GetById(1886)).Returns(workTrackingSystemConnection);
            workTrackingConnectorFactoryMock.Setup(x => x.GetWorkTrackingConnector(workTrackingSystemConnection.WorkTrackingSystem)).Returns(workTrackingConnectorServiceMock.Object);
            workTrackingConnectorServiceMock.Setup(x => x.ValidatePortfolioSettings(It.IsAny<Portfolio>())).ReturnsAsync(expectedResult);

            var subject = CreateSubject();

            var response = await subject.ValidatePortfolioSettings(portfolioSettingDto);

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
        public async Task ValidatePortfolioSettings_WorkTrackingSystemNotFound_ReturnsNotFound()
        {
            var portfolioSettingDto = new PortfolioSettingDto { WorkTrackingSystemConnectionId = 1886 };

            workTrackingSystemConnectionRepoMock.Setup(x => x.GetById(1886)).Returns((WorkTrackingSystemConnection)null);

            var subject = CreateSubject();

            var response = await subject.ValidatePortfolioSettings(portfolioSettingDto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundObjectResult = response.Result as NotFoundResult;
                Assert.That(notFoundObjectResult.StatusCode, Is.EqualTo(404));
            }
        }

        private PortfoliosController CreateSubject()
        {
            return new PortfoliosController(
                portfolioRepoMock.Object,
                teamRepoMock.Object,
                portfolioUpdaterMock.Object,
                workTrackingConnectorFactoryMock.Object,
                workTrackingSystemConnectionRepoMock.Object
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
