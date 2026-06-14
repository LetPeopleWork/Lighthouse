using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesChartOverlayIntegrationTest
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<ITeamMetricsService> teamMetricsServiceMock = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private int seededTeamId;

        private static readonly DateTime WindowStart = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime WindowEnd = new(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamMetricsServiceMock
                .Setup(s => s.GetThroughputProcessBehaviourChart(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(ThreeDayReadyChart());

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ITeamMetricsService>();
                        services.AddScoped(_ => teamMetricsServiceMock.Object);
                        services.RemoveAll<IPortfolioMetricsService>();
                        services.AddScoped(_ => Mock.Of<IPortfolioMetricsService>());
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            seededTeamId = SeedTeam();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task GetThroughputPbc_WithRecurringRuleDay_AnnotatesTheMatchingDataPointAsBlackout()
        {
            await ConfigureRecurringRuleOnSecondDay();

            var chart = await GetThroughputProcessBehaviourChart();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chart.DataPoints[0].IsBlackout, Is.False);
                Assert.That(chart.DataPoints[1].IsBlackout, Is.True,
                    "A recurring-rule day must be annotated on chart overlays exactly like a one-off blackout day (D4 unified evaluation).");
                Assert.That(chart.DataPoints[2].IsBlackout, Is.False);
            }
        }

        [Test]
        public async Task GetThroughputPbc_RecurringRuleDay_AnnotatesIdenticallyToEquivalentOneOffPeriod()
        {
            ConfigureOneOffBlackoutPeriodOnSecondDay();
            var oneOffFlags = (await GetThroughputProcessBehaviourChart()).DataPoints.Select(d => d.IsBlackout).ToArray();

            RemoveAllOneOffPeriods();
            await ConfigureRecurringRuleOnSecondDay();
            var recurringFlags = (await GetThroughputProcessBehaviourChart()).DataPoints.Select(d => d.IsBlackout).ToArray();

            Assert.That(recurringFlags, Is.EqualTo(oneOffFlags),
                "The IsBlackout annotation for a recurring-rule day must match the annotation for an equivalent one-off period.");
        }

        [Test]
        public async Task GetThroughputPbc_NoRecurringRulesAndNoOneOffPeriods_NoDataPointMarkedAsBlackout()
        {
            var chart = await GetThroughputProcessBehaviourChart();

            Assert.That(chart.DataPoints.Any(d => d.IsBlackout), Is.False,
                "With no rules and no periods no overlay is annotated (inherits #4974 D6).");
        }

        private async Task ConfigureRecurringRuleOnSecondDay()
        {
            var secondDay = DateOnly.FromDateTime(WindowStart.AddDays(1));
            client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { secondDay.DayOfWeek.ToString() },
                intervalWeeks = 1,
                start = secondDay.ToString("yyyy-MM-dd"),
                end = secondDay.ToString("yyyy-MM-dd"),
                description = "Single recurring day",
            };

            var response = await client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await response.Content.ReadAsStringAsync());
        }

        private void ConfigureOneOffBlackoutPeriodOnSecondDay()
        {
            var secondDay = DateOnly.FromDateTime(WindowStart.AddDays(1));
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            repository.Add(new BlackoutPeriod { Start = secondDay, End = secondDay, Description = "Company shutdown" });
            repository.Save().GetAwaiter().GetResult();
        }

        private void RemoveAllOneOffPeriods()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            foreach (var period in repository.GetAll().ToList())
            {
                repository.Remove(period);
            }
            repository.Save().GetAwaiter().GetResult();
        }

        private async Task<ProcessBehaviourChart> GetThroughputProcessBehaviourChart()
        {
            client.AsTeamViewer(seededTeamId);
            var startDate = WindowStart.ToString("yyyy-MM-dd");
            var endDate = WindowEnd.ToString("yyyy-MM-dd");

            var response = await client.GetAsync(
                $"/api/latest/teams/{seededTeamId}/metrics/throughput/pbc?startDate={startDate}&endDate={endDate}");

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            return JsonSerializer.Deserialize<ProcessBehaviourChart>(body, JsonOptions)!;
        }

        private static ProcessBehaviourChart ThreeDayReadyChart()
        {
            return new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.Date,
                Average = 5,
                UpperNaturalProcessLimit = 10,
                LowerNaturalProcessLimit = 0,
                DataPoints =
                [
                    new ProcessBehaviourChartDataPoint(WindowStart.ToString("yyyy-MM-dd"), 3, [], [1]),
                    new ProcessBehaviourChartDataPoint(WindowStart.AddDays(1).ToString("yyyy-MM-dd"), 5, [], [2]),
                    new ProcessBehaviourChartDataPoint(WindowStart.AddDays(2).ToString("yyyy-MM-dd"), 4, [], [3]),
                ],
            };
        }

        private int SeedTeam()
        {
            using var scope = factory.Services.CreateScope();
            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
            };
            var team = new Team { Name = $"Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection };
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }
    }
}
