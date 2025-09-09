using Lighthouse.Backend.API;
using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class DemoControllerTest
    {
        private static int idCounter = 0;

        private Mock<IDemoDataService> demoDataServiceMock;

        private List<DemoDataScenario> scenarios;

        [SetUp]
        public void SetUp()
        {
            demoDataServiceMock = new Mock<IDemoDataService>();
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
        public async Task LoadAll_LoadsAllScenarios()
        {
            var scenario1 = CreateScenario("Una");
            var scenario2 = CreateScenario("Una Mas");

            var subject = CreateSubject();

            var response = await subject.LoadAll();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response, Is.InstanceOf<OkResult>());

                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                demoDataServiceMock.Verify(x => x.LoadScenarios(scenario1, scenario2));
            }
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
            return new DemoController(demoDataServiceMock.Object);
        }
    }
}
