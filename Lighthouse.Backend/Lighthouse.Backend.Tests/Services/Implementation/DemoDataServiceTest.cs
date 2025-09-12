using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class DemoDataServiceTest
    {
        private Mock<IRepository<Project>> projectRepoMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionsRepoMock;

        private Mock<IDemoDataFactory> demoDataFactoryMock;

        [SetUp]
        public void SetUp()
        {
            projectRepoMock = new Mock<IRepository<Project>>();
            teamRepoMock = new Mock<IRepository<Team>>();
            workTrackingSystemConnectionsRepoMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            demoDataFactoryMock = new Mock<IDemoDataFactory>();

            projectRepoMock.Setup(x => x.GetAll()).Returns(new List<Project>());
            teamRepoMock.Setup(x => x.GetAll()).Returns(new List<Team>());
            workTrackingSystemConnectionsRepoMock.Setup(x => x.GetAll()).Returns(new List<WorkTrackingSystemConnection>());

            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(new WorkTrackingSystemConnection { Id = 18 });
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(new Team { Id = 86 });
            demoDataFactoryMock.Setup(x => x.CreateDemoProject(It.IsAny<string>())).Returns(new Project { Id = 42 });
        }

        [Test]
        [TestCase("When Will This Be Done?", 1, 1)]
        [TestCase("Crash Override", 1, 1)]
        [TestCase("Watermelon", 1, 1)]
        [TestCase("Product Launch", 2, 1)]
        public void LoadScenarios_ReturnsFreeScenarios(string scenarioName, int teams, int projects)
        {
            var subject = CreateSubject();

            var scenarios = subject.GetAllScenarios();

            using (Assert.EnterMultipleScope())
            {
                var scenario = scenarios.SingleOrDefault(x => x.Title == scenarioName);
                Assert.That(scenario, Is.Not.Null);
                Assert.That(scenario.IsPremium, Is.False);
                Assert.That(scenario.NumberOfTeams, Is.EqualTo(teams));
                Assert.That(scenario.NumberOfProjects, Is.EqualTo(projects));
            }
        }

        [Test]
        public async Task LoadScenario_RemovesExistingProjects()
        {
            var projects = new List<Project>
            {
                new Project{ Id = 1 },
                new Project{ Id = 2 },
                new Project{ Id = 3 },
            };

            projectRepoMock.Setup(x => x.GetAll()).Returns(projects);

            var subject = CreateSubject();

            var demoScenario = subject.GetAllScenarios().First();

            await subject.LoadScenarios([demoScenario]);

            projectRepoMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Exactly(3));
            projectRepoMock.Verify(x => x.Save());
        }

        [Test]
        public async Task LoadScenario_RemovesExistingTeams()
        {
            var teams = new List<Team>
            {
                new Team{ Id = 1 },
                new Team{ Id = 2 },
                new Team{ Id = 3 },
            };

            teamRepoMock.Setup(x => x.GetAll()).Returns(teams);

            var subject = CreateSubject();

            var demoScenario = subject.GetAllScenarios().First();

            await subject.LoadScenarios([demoScenario]);

            teamRepoMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Exactly(3));
            teamRepoMock.Verify(x => x.Save());
        }

        [Test]
        public async Task LoadScenario_RemovesAllWorkTrackingSystemConnections()
        {
            var workTrackingSystemConnections = new List<WorkTrackingSystemConnection>
            {
                new WorkTrackingSystemConnection{ Id = 1 },
                new WorkTrackingSystemConnection{Id = 2},
            };

            workTrackingSystemConnectionsRepoMock.Setup(x => x.GetAll()).Returns(workTrackingSystemConnections);

            var subject = CreateSubject();

            var demoScenario = subject.GetAllScenarios().First();

            await subject.LoadScenarios([demoScenario]);

            workTrackingSystemConnectionsRepoMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Exactly(2));
            workTrackingSystemConnectionsRepoMock.Verify(x => x.Save());
        }

        [Test]
        public async Task LoadScenario_AddsDemoDataWorkTrackingConnection()
        {
            var expectedWorkTrackingConnection = new WorkTrackingSystemConnection { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(expectedWorkTrackingConnection);

            var subject = CreateSubject();

            var demoScenario = subject.GetAllScenarios().First();
            await subject.LoadScenarios([demoScenario]);

            workTrackingSystemConnectionsRepoMock.Verify(x => x.Add(expectedWorkTrackingConnection));
            workTrackingSystemConnectionsRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task LoadScenario_AddsTeamsForScenarios()
        {
            var demoTeam = new Team { Id = 12 };
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(demoTeam);

            var expectedWorkTrackingConnection = new WorkTrackingSystemConnection { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(expectedWorkTrackingConnection);

            var subject = CreateSubject();

            var demoScenario = subject.GetAllScenarios().First();
            demoScenario.Projects.Clear();

            await subject.LoadScenarios([demoScenario]);

            teamRepoMock.Verify(x => x.Add(demoTeam), Times.Once);
            teamRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task LoadScenario_TwoScenariosWithSameTeam_AddsTeamOnce()
        {
            var demoTeam = new Team { Id = 12 };
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(demoTeam);

            var expectedWorkTrackingConnection = new WorkTrackingSystemConnection { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(expectedWorkTrackingConnection);

            var subject = CreateSubject();

            var demoScenarios = subject.GetAllScenarios().Take(2).ToArray();
            demoScenarios[0].Teams.Clear();
            demoScenarios[0].Teams.Add("DEMO");
            demoScenarios[1].Teams.Clear();
            demoScenarios[1].Teams.Add("DEMO");

            await subject.LoadScenarios(demoScenarios);

            teamRepoMock.Verify(x => x.Add(demoTeam), Times.Once);
        }

        [Test]
        public async Task LoadScenario_TwoScenariosWithDifferentTeams_AddsTeamOnce()
        {
            var demoTeam = new Team { Id = 12 };
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(demoTeam);

            var expectedWorkTrackingConnection = new WorkTrackingSystemConnection { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(expectedWorkTrackingConnection);

            var subject = CreateSubject();

            var demoScenarios = subject.GetAllScenarios().Take(2).ToArray();
            demoScenarios[0].Teams.Clear();
            demoScenarios[0].Teams.Add("DEMO");
            demoScenarios[1].Teams.Clear();
            demoScenarios[1].Teams.Add("Hello");
            demoScenarios[1].Teams.Add("DEMO");

            await subject.LoadScenarios(demoScenarios);

            teamRepoMock.Verify(x => x.Add(demoTeam), Times.Exactly(2));
        }

        [Test]
        public async Task LoadScenario_AddsProjectsForScenarios()
        {
            var demoTeam = new Team { Id = 12 };
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(demoTeam);

            var demoProject = new Project { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoProject(It.IsAny<string>())).Returns(demoProject);

            var expectedWorkTrackingConnection = new WorkTrackingSystemConnection { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(expectedWorkTrackingConnection);

            var subject = CreateSubject();

            var demoScenario = subject.GetAllScenarios().First();

            await subject.LoadScenarios([demoScenario]);

            projectRepoMock.Verify(x => x.Add(demoProject), Times.Once);
            projectRepoMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        public async Task LoadScenario_TwoScenariosWithSameProjects_AddsProjectsJustOnce()
        {
            var demoTeam = new Team { Id = 12 };
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(demoTeam);

            var demoProject = new Project { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoProject(It.IsAny<string>())).Returns(demoProject);

            var expectedWorkTrackingConnection = new WorkTrackingSystemConnection { Id = 1 };
            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(expectedWorkTrackingConnection);

            var subject = CreateSubject();

            var demoScenarios = subject.GetAllScenarios().Take(2).ToArray();
            demoScenarios[0].Teams.Clear();
            demoScenarios[0].Teams.Add("DEMO");
            demoScenarios[0].Projects.Clear();
            demoScenarios[0].Projects.Add("DEMO PROJECT");
            
            demoScenarios[1].Teams.Clear();
            demoScenarios[1].Teams.Add("DEMO");
            demoScenarios[1].Projects.Clear();
            demoScenarios[1].Projects.Add("DEMO PROJECT");
            demoScenarios[1].Projects.Add("OTHER PROJECT");

            await subject.LoadScenarios(demoScenarios);

            projectRepoMock.Verify(x => x.Add(demoProject), Times.Exactly(2));
        }

        private DemoDataService CreateSubject()
        {
            return new DemoDataService(projectRepoMock.Object, teamRepoMock.Object, workTrackingSystemConnectionsRepoMock.Object, demoDataFactoryMock.Object);
        }
    }
}
