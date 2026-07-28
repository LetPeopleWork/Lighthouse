using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class DemoDataServiceTest
    {
        private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        private Mock<IRepository<Portfolio>> projectRepoMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionsRepoMock;
        private Mock<IDeliveryRepository> deliveryRepoMock;
        private Mock<IDeliveryMetricSnapshotRepository> deliveryMetricSnapshotRepoMock;

        private Mock<IDemoDataFactory> demoDataFactoryMock;

        [SetUp]
        public void SetUp()
        {
            projectRepoMock = new Mock<IRepository<Portfolio>>();
            teamRepoMock = new Mock<IRepository<Team>>();
            workTrackingSystemConnectionsRepoMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            deliveryRepoMock = new Mock<IDeliveryRepository>();
            deliveryMetricSnapshotRepoMock = new Mock<IDeliveryMetricSnapshotRepository>();
            demoDataFactoryMock = new Mock<IDemoDataFactory>();

            projectRepoMock.Setup(x => x.GetAll()).Returns(new List<Portfolio>());
            teamRepoMock.Setup(x => x.GetAll()).Returns(new List<Team>());
            workTrackingSystemConnectionsRepoMock.Setup(x => x.GetAll()).Returns(new List<WorkTrackingSystemConnection>());
            deliveryMetricSnapshotRepoMock
                .Setup(x => x.GetOrCreateForDay(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .Returns((int deliveryId, DateOnly day) => new DeliveryMetricSnapshot
                {
                    DeliveryId = deliveryId,
                    RecordedDay = day,
                    RecordedAt = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                });

            demoDataFactoryMock.Setup(x => x.CreateDemoWorkTrackingSystemConnection()).Returns(new WorkTrackingSystemConnection { Id = 18 });
            demoDataFactoryMock.Setup(x => x.CreateDemoTeam(It.IsAny<string>())).Returns(new Team { Id = 86 });
            demoDataFactoryMock.Setup(x => x.CreateDemoProject(It.IsAny<string>())).Returns(new Portfolio { Id = 42, Name = "Projecto" });
        }

        [Test]
        [TestCase("When Will This Be Done?", "One Team, one project with a a set of Epics, and the question: When can we get it?")]
        [TestCase("Too Much WIP", "A team that is super busy, but progress is slow.")]
        [TestCase("Product Launch", "Two teams, one product they work on together. When can we launch?")]
        public void LoadScenarios_ReturnsFreeScenarios(string scenarioName, string description)
        {
            var subject = CreateSubject();

            var scenarios = subject.GetAllScenarios();

            using (Assert.EnterMultipleScope())
            {
                var scenario = scenarios.SingleOrDefault(x => x.Title == scenarioName);
                Assert.That(scenario, Is.Not.Null);
                Assert.That(scenario.Description, Is.EqualTo(description));
                Assert.That(scenario.IsPremium, Is.False);
            }
        }

        [Test]
        public void LoadScenarios_DependenciesScenario_CoversEveryForecastDataState()
        {
            // The scenario carries one team with no throughput at all and one with too little to trust,
            // so its Epics demonstrate the unknown-forecast state (ADR-112) and the insufficient-data
            // signal side by side. Removing either team would quietly take the demo coverage with it.
            var subject = CreateSubject();

            var dependencies = subject.GetAllScenarios().Single(x => x.Title == "Dependencies");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dependencies.Teams, Does.Contain("Team Meridian"));
                Assert.That(dependencies.Teams, Does.Contain("Team Equinox"));
            }
        }

        [Test]
        [TestCase("Flow in Scrum", "A team that is focusing on delivering at the end of their Sprint. What does it do to their flow?")]
        [TestCase("It's Not Always What It Seems", "Two teams that look a certain way on first glance. Explore all Flow Metrics to get a full picture and don't draw conclusions before.")]
        [TestCase("Dependencies", "Explore a project where we have Epics with multiple Teams involved.")]
        [TestCase("Quarterly Planning", "See how a Quarterly Planning could look like for a Team that uses Monte Carlo Forecasts")]
        public void LoadScenarios_ReturnsPremiumScenarios(string scenarioName, string description)
        {
            var subject = CreateSubject();

            var scenarios = subject.GetAllScenarios();

            using (Assert.EnterMultipleScope())
            {
                var scenario = scenarios.SingleOrDefault(x => x.Title == scenarioName);
                Assert.That(scenario, Is.Not.Null);
                Assert.That(scenario.Description, Is.EqualTo(description));
                Assert.That(scenario.IsPremium, Is.True);
            }
        }

        [Test]
        public async Task LoadScenario_RemovesExistingProjects()
        {
            var projects = new List<Portfolio>
            {
                new Portfolio{ Id = 1 },
                new Portfolio{ Id = 2 },
                new Portfolio{ Id = 3 },
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

            var demoProject = new Portfolio { Id = 1, Name = "Project64" };
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

            var demoProject = new Portfolio { Id = 1, Name = "Project64" };
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

        /// <summary>
        /// Bug #5567: 23:30 UTC is already the next day in Zurich. The burnup the demo seeds must
        /// land on the same days the migrated read paths report, otherwise the newest snapshot is
        /// "yesterday" for the first two hours of every instance day.
        /// </summary>
        [Test]
        public async Task LoadScenario_PastInstanceMidnightButBeforeUtcMidnight_SeedsBurnupOnTheInstanceDays()
        {
            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 3, 10, 23, 30, 0, TimeSpan.Zero), Zurich);

            var apollo = new Portfolio { Id = 7, Name = "Project Apollo" };
            demoDataFactoryMock.Setup(x => x.CreateDemoProject("Project Apollo")).Returns(apollo);

            var recordedDays = new List<DateOnly>();
            deliveryMetricSnapshotRepoMock
                .Setup(x => x.GetOrCreateForDay(It.IsAny<int>(), It.IsAny<DateOnly>()))
                .Returns((int deliveryId, DateOnly day) =>
                {
                    recordedDays.Add(day);
                    return new DeliveryMetricSnapshot { DeliveryId = deliveryId, RecordedDay = day };
                });

            Delivery? seededDelivery = null;
            deliveryRepoMock.Setup(x => x.Add(It.IsAny<Delivery>())).Callback((Delivery delivery) => seededDelivery = delivery);

            var subject = CreateSubject(clock);

            var apolloScenario = subject.GetAllScenarios().Single(scenario => scenario.Projects.Contains("Project Apollo"));
            await subject.LoadScenarios([apolloScenario]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seededDelivery, Is.Not.Null);
                Assert.That(seededDelivery.Date, Is.EqualTo(clock.Today.AddDays(14).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));
                Assert.That(recordedDays.Max(), Is.EqualTo(clock.Today));
                Assert.That(recordedDays.Min(), Is.EqualTo(clock.Today.AddDays(-14)));
            }
        }

        private DemoDataService CreateSubject(ILighthouseClock? clock = null)
        {
            return new DemoDataService(projectRepoMock.Object, teamRepoMock.Object, workTrackingSystemConnectionsRepoMock.Object, deliveryRepoMock.Object, deliveryMetricSnapshotRepoMock.Object, demoDataFactoryMock.Object, clock ?? new FakeLighthouseClock(DateTimeOffset.UtcNow));
        }
    }
}
