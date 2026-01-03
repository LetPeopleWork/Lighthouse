using Lighthouse.Backend.MCP;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Moq;
using Newtonsoft.Json;

namespace Lighthouse.Backend.Tests.MCP
{
    public class LighthouseTeamToolsTest : LighthosueToolsBaseTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            forecastServiceMock = new Mock<IForecastService>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();

            SetupServiceProviderMock(teamRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(teamMetricsServiceMock.Object);
        }

        [Test]
        public void GetAllTeams_ReturnsIdAndName()
        {
            var team = CreateTeam();

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(new List<Team> { team });

            var subject = CreateSubject();
            var result = subject.GetAllTeams();

            using (Assert.EnterMultipleScope())
            {
                var teams = (JsonConvert.DeserializeObject<IEnumerable<dynamic>>(result) ?? System.Linq.Enumerable.Empty<dynamic>()).ToList();

                Assert.That(teams, Has.Count.EqualTo(1));

                var teamToVerify = teams.Single();
                int teamId = Convert.ToInt32(teamToVerify.Id);
                string teamName = teamToVerify.Name;

                Assert.That(teamId, Is.EqualTo(1));
                Assert.That(teamName, Is.EqualTo("Test Team"));
            };
        }

        [Test]
        public void GetTeamByName_TeamDoesNotExist_ReturnsErrorMessage()
        {
            var subject = CreateSubject();

            var result = subject.GetTeamByName("NonExistentTeam");

            Assert.That(result, Is.EqualTo("No team found with name NonExistentTeam"));
        }

        [Test]
        [TestCase("Test Team")]
        [TestCase("Test")]
        [TestCase("Team")]
        [TestCase("test team")]
        [TestCase("TEST TEAM")]
        [TestCase("TeSt TeAm")]
        public void GetTeamByName_TeamExists_ReturnsTeamDetails(string teamName)
        {
            var team = CreateTeam();
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns((Func<Team, bool> func) => new List<Team> { team }.SingleOrDefault(func));

            var subject = CreateSubject();
            var result = subject.GetTeamByName(teamName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                var teamDetails = JsonConvert.DeserializeObject<Team>(result);
                Assert.That(teamDetails.Id, Is.EqualTo(1));
                Assert.That(teamDetails.Name, Is.EqualTo("Test Team"));
                Assert.That(teamDetails.FeatureWIP, Is.EqualTo(3));
                Assert.That(teamDetails.AutomaticallyAdjustFeatureWIP, Is.True);
            };
        }

        [Test]
        public void RunHowManyForecast_TeamDoesNotExist_ReturnsErrorMessage()
        {
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns((Team?)null);

            var subject = CreateSubject();
            var result = subject.RunHowManyForecast("NonExistentTeam", DateTime.Today.AddDays(30));

            Assert.That(result, Is.EqualTo("No team found with name NonExistentTeam"));
        }

        [Test]
        public void RunHowManyForecast_WithValidTeam_ReturnsForecasts()
        {
            var team = CreateTeam();
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns(team);

            var throughput = new RunChartData();
            teamMetricsServiceMock.Setup(x => x.GetCurrentThroughputForTeam(team)).Returns(throughput);

            var targetDate = DateTime.Today.AddDays(14);
            var expectedDays = 14;

            var expectedForecast = new HowManyForecast(new Dictionary<int, int> { { 10, 50 }, { 15, 85 } }, expectedDays);
            forecastServiceMock.Setup(x => x.HowMany(throughput, expectedDays)).Returns(expectedForecast);

            var subject = CreateSubject();
            var result = subject.RunHowManyForecast("Test Team", targetDate);

            var forecast = JsonConvert.DeserializeObject<HowManyForecast>(result);
            Assert.That(forecast, Is.Not.Null);
        }

        [Test]
        public void RunWhenForecast_TeamDoesNotExist_ReturnsErrorMessage()
        {
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns((Team?)null);

            var subject = CreateSubject();
            var result = subject.RunWhenForecast("NonExistentTeam", 10);

            Assert.That(result, Is.EqualTo("No team found with name NonExistentTeam"));
        }

        [Test]
        public void RunWhenForecast_WithValidTeam_ReturnsForecasts()
        {
            var team = CreateTeam();
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns(team);

            var expectedForecast = new WhenForecast(new SimulationResult(team, new Feature(), 10));
            forecastServiceMock.Setup(x => x.When(team, 10)).ReturnsAsync(expectedForecast);

            var subject = CreateSubject();
            var result = subject.RunWhenForecast("Test Team", 10);

            var forecast = JsonConvert.DeserializeObject<WhenForecast>(result);
            Assert.That(forecast, Is.Not.Null);
        }

        [Test]
        public void GetFlowMetricsForTeam_TeamDoesNotExist_ReturnsErrorMessage()
        {
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns((Team?)null);

            var subject = CreateSubject();
            var result = subject.GetFlowMetricsForTeam("NonExistentTeam", null, null);

            Assert.That(result, Is.EqualTo("No team found with name NonExistentTeam"));
        }

        [Test]
        public void GetFlowMetricsForTeam_ReturnsMetrics()
        {
            var team = CreateTeam();
            teamRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Team, bool>>())).Returns(team);

            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            var expectedPercentiles = new List<PercentileValue> { new PercentileValue(50, 5), new PercentileValue(80, 2) };
            teamMetricsServiceMock.Setup(x => x.GetCycleTimePercentilesForTeam(team, startDate, endDate)).Returns(expectedPercentiles);

            var closedItems = new List<WorkItem> { new WorkItem { StartedDate = DateTime.Today.AddDays(-1), ClosedDate = DateTime.Today, StateCategory = StateCategories.Done } };
            teamMetricsServiceMock.Setup(x => x.GetClosedItemsForTeam(team, startDate, endDate)).Returns(closedItems);

            var wipOverTime = new RunChartData(RunChartDataGenerator.GenerateRunChartData([1, 2, 3, 4, 5]));
            teamMetricsServiceMock.Setup(x => x.GetWorkInProgressOverTimeForTeam(team, startDate, endDate)).Returns(wipOverTime);

            var throughput = new RunChartData(RunChartDataGenerator.GenerateRunChartData([10, 11, 12, 13, 14]));
            teamMetricsServiceMock.Setup(x => x.GetThroughputForTeam(team, startDate, endDate)).Returns(throughput);

            var subject = CreateSubject();
            var result = subject.GetFlowMetricsForTeam("Test Team", startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                var metrics = JsonConvert.DeserializeObject<dynamic>(result);
                Assert.That(metrics, Is.Not.Null);

                var actualPercentiles = ((Newtonsoft.Json.Linq.JArray)metrics.cycleTimePercentiles)
                    .Select(item => item.ToObject<PercentileValue>()).ToList();

                Assert.That(actualPercentiles, Has.Count.EqualTo(2));
                Assert.That(actualPercentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(actualPercentiles[0].Value, Is.EqualTo(5));
                Assert.That(actualPercentiles[1].Percentile, Is.EqualTo(80));
                Assert.That(actualPercentiles[1].Value, Is.EqualTo(2));

                var cycleTimes = ((Newtonsoft.Json.Linq.JArray)metrics.cycleTimes)
                    .Select(item => item.ToObject<int>()).ToList();
                Assert.That(cycleTimes, Has.Count.EqualTo(1));
                Assert.That(cycleTimes.Single(), Is.EqualTo(2));

                var actualWip = JsonConvert.DeserializeObject<RunChartData>(metrics.wip.ToString());
                Assert.That(actualWip, Is.Not.Null);
                Assert.That(actualWip.History, Is.EqualTo(wipOverTime.History));
                Assert.That(actualWip.Total, Is.EqualTo(wipOverTime.Total));

                var actualThroughput = JsonConvert.DeserializeObject<RunChartData>(metrics.throughput.ToString());
                Assert.That(actualThroughput, Is.Not.Null);
                Assert.That(actualThroughput.History, Is.EqualTo(throughput.History));
                Assert.That(actualThroughput.Total, Is.EqualTo(throughput.Total));
            };
        }

        private Team CreateTeam()
        {
            return new Team
            {
                Id = 1,
                Name = "Test Team",
                FeatureWIP = 3,
                AutomaticallyAdjustFeatureWIP = true,
            };
        }

        private LighthouseTeamTools CreateSubject()
        {
            return new LighthouseTeamTools(ServiceScopeFactory);
        }
    }
}