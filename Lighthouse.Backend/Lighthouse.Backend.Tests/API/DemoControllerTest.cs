using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class DemoControllerTest
    {
        private static int idCounter = 0;

        private Mock<IDemoDataService> demoDataServiceMock;

        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<IRepository<Portfolio>> projectRepoMock;
        private Mock<ITeamUpdater> teamUpdaterMock;
        private Mock<IProjectUpdater> projectUpdaterMock;

        private List<DemoDataScenario> scenarios;

        [SetUp]
        public void SetUp()
        {
            demoDataServiceMock = new Mock<IDemoDataService>();

            teamRepoMock = new Mock<IRepository<Team>>();
            projectRepoMock = new Mock<IRepository<Portfolio>>();
            teamUpdaterMock = new Mock<ITeamUpdater>();
            projectUpdaterMock = new Mock<IProjectUpdater>();

            teamRepoMock.Setup(x => x.GetAll()).Returns(new List<Team>());
            projectRepoMock.Setup(x => x.GetAll()).Returns(new List<Portfolio>());

            scenarios = new List<DemoDataScenario>();

            demoDataServiceMock.Setup(x => x.GetAllScenarios()).Returns(scenarios);
        }

        [Test]
        public void GetScenarios_ReturnsAllScenariosFromDemoDataService()
        {
            var scenario1 = CreateScenario("Scenario 1");
            var scenario2 = CreateScenario("Scenario 2");
            scenario2.IsPremium = true;

            var subject = CreateSubject();

            var response = subject.GetScenarios();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var actualScenarios = okResult.Value as List<DemoDataScenario>;
                Assert.That(actualScenarios, Has.Count.EqualTo(2));

                Assert.That(actualScenarios[0], Is.EqualTo(scenario1));
                Assert.That(actualScenarios[1], Is.EqualTo(scenario2));
            }
        }

        [Test]
        public async Task LoadScenario_ScenarioDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = await subject.LoadScenario(12);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<NotFoundResult>());

                var okResult = response as NotFoundResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(404));
            }
        }

        [Test]
        public async Task LoadScenario_ScenarioExists_Loads()
        {
            var scenario = CreateScenario("My Scenario");

            var subject = CreateSubject();

            var response = await subject.LoadScenario(scenario.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());
                
                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                demoDataServiceMock.Verify(x => x.LoadScenarios(scenario));
            }
        }

        [Test]
        public async Task LoadScenario_UpdatesAllTeams()
        {
            var team1 = new Team { Id = 1 };
            var team2 = new Team { Id = 2 };

            teamRepoMock.Setup(x => x.GetAll()).Returns(new List<Team> { team1, team2 });

            var scenario = CreateScenario("Una");

            var subject = CreateSubject();

            await subject.LoadScenario(scenario.Id);

            teamUpdaterMock.Verify(x => x.TriggerUpdate(1));
            teamUpdaterMock.Verify(x => x.TriggerUpdate(2));
        }

        [Test]
        public async Task LoadScenario_UpdatesAllProjects()
        {

            var project1 = new Portfolio { Id = 1 };
            var project2 = new Portfolio { Id = 2 };

            projectRepoMock.Setup(x => x.GetAll()).Returns(new List<Portfolio> { project1, project2 });

            var scenario = CreateScenario("Una");

            var subject = CreateSubject();

            await subject.LoadScenario(scenario.Id);

            projectUpdaterMock.Verify(x => x.TriggerUpdate(1));
            projectUpdaterMock.Verify(x => x.TriggerUpdate(2));
        }

        private DemoDataScenario CreateScenario(string scenarioName)
        {
            var scenario = new DemoDataScenario
            {
                Id = idCounter++,
                Title = scenarioName,
            };

            scenarios.Add(scenario);

            return scenario;
        }

        private DemoController CreateSubject()
        {
            return new DemoController(demoDataServiceMock.Object, teamRepoMock.Object, teamUpdaterMock.Object, projectRepoMock.Object, projectUpdaterMock.Object);
        }
    }
}
