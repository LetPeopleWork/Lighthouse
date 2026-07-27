using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Bug #5567 Finding F: the window bounds and the item bucketing must agree on what a day is.
    /// Fixing only the bounds relocates the off-by-one instead of removing it.
    /// </summary>
    [TestFixture]
    public class InstanceDayItemBucketingTest
    {
        private static readonly TimeZoneInfo Zurich = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        // 00:30 on Jul 28 in Zurich, still Jul 27 in UTC.
        private static readonly DateTime LateEveningUtc = new(2026, 7, 27, 22, 30, 0, DateTimeKind.Utc);

        private static readonly DateTime Jul27 = new(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul28 = new(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Aug03 = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul21 = new(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc);

        private FakeLighthouseClock clock;
        private List<WorkItem> workItems;
        private List<Feature> features;
        private Team team;
        private Portfolio portfolio;
        private TeamMetricsService teamMetrics;
        private PortfolioMetricsService portfolioMetrics;

        [SetUp]
        public void Setup()
        {
            clock = new FakeLighthouseClock(new DateTimeOffset(2026, 7, 28, 6, 0, 0, TimeSpan.Zero), Zurich);
            workItems = [];
            features = [];
            team = new Team { Id = 1, Name = "Zurich Team", ThroughputHistory = 30 };
            portfolio = new Portfolio { Id = 1, Name = "Zurich Portfolio" };

            teamMetrics = BuildTeamMetricsService();
            portfolioMetrics = BuildPortfolioMetricsService();
        }

        [TearDown]
        public void TearDown()
        {
            teamMetrics.InvalidateTeamMetrics(team);
            portfolioMetrics.InvalidatePortfolioMetrics(portfolio);
        }

        [Test]
        public void ItemClosedLateInTheEveningLocal_BucketsIntoTheInstanceDay_NotThePreviousUtcDay()
        {
            AddClosedWorkItem(LateEveningUtc);

            var throughput = teamMetrics.GetThroughputForTeam(team, Jul27, Jul28);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.GetCountOnDay(0), Is.Zero,
                    "22:30Z on Jul 27 is already Jul 28 in Zurich - it must not count on the Jul 27 bucket.");
                Assert.That(throughput.GetCountOnDay(1), Is.EqualTo(1),
                    "T1b: the item belongs to instance day Jul 28.");
            }
        }

        [Test]
        public void ItemOnTheWindowBoundary_IsCountedExactlyOnce_AcrossTwoAdjacentWindows()
        {
            AddClosedWorkItem(LateEveningUtc);

            var earlierWindow = teamMetrics.GetThroughputForTeam(team, Jul21, Jul27);
            teamMetrics.InvalidateTeamMetrics(team);
            var laterWindow = teamMetrics.GetThroughputForTeam(team, Jul28, Aug03);

            var totalAcrossBothWindows = earlierWindow.Total + laterWindow.Total;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(totalAcrossBothWindows, Is.EqualTo(1),
                    "Both ends must use the same definition of a day: never dropped, never double-counted.");
                Assert.That(laterWindow.Total, Is.EqualTo(1), "The later window owns instance day Jul 28.");
            }
        }

        [Test]
        public void ClosedItemsForTeam_ReduceTheStoredInstantToTheInstanceDay()
        {
            AddClosedWorkItem(LateEveningUtc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamMetrics.GetClosedItemsForTeam(team, Jul27, Jul27), Is.Empty);
                Assert.That(teamMetrics.GetClosedItemsForTeam(team, Jul28, Jul28), Has.Exactly(1).Items);
            }
        }

        [Test]
        public void WorkInProgressPopulation_TreatsTheClosingInstantAsTheInstanceDay()
        {
            var item = AddClosedWorkItem(LateEveningUtc);
            item.StartedDate = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc);

            var wipOverTime = teamMetrics.GetWorkInProgressOverTimeForTeam(team, Jul27, Jul28);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wipOverTime.GetCountOnDay(0), Is.EqualTo(1),
                    "The item is still open on Jul 27 in Zurich - it closes after midnight local.");
                Assert.That(wipOverTime.GetCountOnDay(1), Is.Zero,
                    "It closed on instance day Jul 28, so it is no longer in progress that day.");
            }
        }

        [Test]
        public void PortfolioCycleTimeData_BucketsTheFeatureIntoTheInstanceDay()
        {
            AddClosedFeature(LateEveningUtc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolioMetrics.GetCycleTimeDataForPortfolio(portfolio, Jul27, Jul27), Is.Empty);
                Assert.That(portfolioMetrics.GetCycleTimeDataForPortfolio(portfolio, Jul28, Jul28), Has.Exactly(1).Items);
            }
        }

        [Test]
        public void PortfolioFeature_OnTheWindowBoundary_IsCountedExactlyOnceAcrossTwoAdjacentWindows()
        {
            AddClosedFeature(LateEveningUtc);

            var earlier = portfolioMetrics.GetCycleTimeDataForPortfolio(portfolio, Jul21, Jul27).Count();
            var later = portfolioMetrics.GetCycleTimeDataForPortfolio(portfolio, Jul28, Aug03).Count();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(earlier + later, Is.EqualTo(1),
                    "Portfolio bucketing must be symmetric with the team side - never zero, never twice.");
                Assert.That(later, Is.EqualTo(1));
            }
        }

        [Test]
        public void PortfolioSizeChart_BucketsTheFeatureIntoTheInstanceDay()
        {
            AddClosedFeature(LateEveningUtc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolioMetrics.GetAllFeaturesForSizeChart(portfolio, Jul27, Jul27), Is.Empty);
                Assert.That(portfolioMetrics.GetAllFeaturesForSizeChart(portfolio, Jul28, Jul28), Has.Exactly(1).Items);
            }
        }

        [Test]
        public void UtcConfiguredInstance_ProducesTheSameAgesAndCycleTimesAsBeforeTheZoneShift()
        {
            var closed = new WorkItem
            {
                Id = 42,
                StateCategory = StateCategories.Done,
                StartedDate = new DateTime(2026, 7, 20, 22, 30, 0, DateTimeKind.Utc),
                ClosedDate = LateEveningUtc,
            };

            var doing = new WorkItem
            {
                Id = 43,
                StateCategory = StateCategories.Doing,
                StartedDate = new DateTime(2026, 7, 20, 22, 30, 0, DateTimeKind.Utc),
            };

            var utcClock = new FakeLighthouseClock(new DateTimeOffset(2026, 7, 28, 6, 0, 0, TimeSpan.Zero));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closed.CycleTime(utcClock.Zone), Is.EqualTo(8),
                    "Decision 3: the inclusive +1 is unchanged and a UTC instance sees no arithmetic change.");
                Assert.That(doing.WorkItemAge(utcClock.Zone, utcClock.Today), Is.EqualTo(9),
                    "Decision 3: WorkItemAge under a UTC instance is byte-identical to HEAD.");
                Assert.That(closed.AgeOnDay(utcClock.Zone, new DateOnly(2026, 7, 24)), Is.EqualTo(5));
            }
        }

        private WorkItem AddClosedWorkItem(DateTime closedAtUtc)
        {
            var item = new WorkItem
            {
                Id = workItems.Count + 1,
                ReferenceId = $"WI-{workItems.Count + 1}",
                TeamId = team.Id,
                Team = team,
                StateCategory = StateCategories.Done,
                StartedDate = closedAtUtc.AddDays(-3),
                ClosedDate = closedAtUtc,
            };

            workItems.Add(item);
            return item;
        }

        private Feature AddClosedFeature(DateTime closedAtUtc)
        {
            var feature = new Feature
            {
                Id = features.Count + 1,
                ReferenceId = $"F-{features.Count + 1}",
                StateCategory = StateCategories.Done,
                StartedDate = closedAtUtc.AddDays(-3),
                ClosedDate = closedAtUtc,
            };

            feature.Portfolios.Add(portfolio);
            features.Add(feature);
            return feature;
        }

        private TeamMetricsService BuildTeamMetricsService()
        {
            var workItemRepository = new Mock<IWorkItemRepository>();
            workItemRepository
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());

            var appSettingService = new Mock<IAppSettingService>();
            appSettingService.Setup(x => x.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 1 });

            var blackoutPeriodService = new Mock<IBlackoutPeriodService>();
            blackoutPeriodService
                .Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);

            var forecastFilterRuleService = new Mock<IForecastFilterRuleService>();
            forecastFilterRuleService
                .Setup(s => s.GetEffectiveRuleSet(It.IsAny<Team>()))
                .Returns((Lighthouse.Backend.Models.WorkItemRules.WorkItemRuleSet?)null);

            return new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepository.Object,
                Mock.Of<IRepository<Feature>>(),
                appSettingService.Object,
                BuildServiceProvider(),
                blackoutPeriodService.Object,
                forecastFilterRuleService.Object,
                Mock.Of<IWorkItemStateTransitionRepository>(),
                clock);
        }

        private PortfolioMetricsService BuildPortfolioMetricsService()
        {
            var featureRepository = new Mock<IRepository<Feature>>();
            featureRepository
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> predicate) => features.Where(predicate.Compile()).AsQueryable());

            var appSettingService = new Mock<IAppSettingService>();
            appSettingService.Setup(x => x.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            return new PortfolioMetricsService(
                Mock.Of<ILogger<PortfolioMetricsService>>(),
                featureRepository.Object,
                appSettingService.Object,
                BuildServiceProvider(),
                Mock.Of<IFeatureStateTransitionRepository>(),
                clock);
        }

        private static IServiceProvider BuildServiceProvider()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(sp => sp.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider
                .Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(Mock.Of<IForecastService>());

            return serviceProvider.Object;
        }
    }
}
