using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Story 5508 (widget-loose-ends) slice 03 — Work Item Age percentiles as of the LAST DAY of the
    /// selected range (DISCUSS D3, DESIGN D13-D16).
    ///
    /// The defect: GetWorkItemAgePercentilesFor{Team,Portfolio} take a date-correct WIP snapshot and
    /// then project it through WorkItemAge, which is today-anchored AND returns 0 unless the item is
    /// Doing right now. Historical items are zeroed and then dropped by the `age > 0` filter.
    ///
    /// CI6 is the anti-drift assertion: the corrected projection must agree with the shipped
    /// GenerateTotalWorkItemAgeByDay reference at the same date, so a second definition of "age on a
    /// day" cannot come into existence.
    /// CI4 is the live-view regression guard: when endDate is today, nothing changes.
    /// </summary>
    [TestFixture]
    public class TeamWorkItemAgeAsOfDateTest
    {
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private TeamMetricsService subject;
        private Team testTeam;
        private List<WorkItem> workItems;

        private static readonly DateTime Jul01 = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul04 = new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul06 = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul10 = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void Setup()
        {
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            var featureRepositoryMock = new Mock<IRepository<Feature>>();
            var blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            blackoutPeriodServiceMock.Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);

            var appSettingsServiceMock = new Mock<IAppSettingService>();
            appSettingsServiceMock.Setup(x => x.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(Mock.Of<IForecastService>());

            var forecastFilterRuleServiceMock = new Mock<IForecastFilterRuleService>();
            forecastFilterRuleServiceMock.Setup(s => s.GetEffectiveRuleSet(It.IsAny<Team>()))
                .Returns((Lighthouse.Backend.Models.WorkItemRules.WorkItemRuleSet?)null);

            testTeam = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30 };
            subject = new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepositoryMock.Object,
                featureRepositoryMock.Object,
                appSettingsServiceMock.Object,
                serviceProvider.Object,
                blackoutPeriodServiceMock.Object,
                forecastFilterRuleServiceMock.Object,
                Mock.Of<IWorkItemStateTransitionRepository>());

            workItems = new List<WorkItem>();
            workItemRepositoryMock
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> pred) => workItems.Where(pred.Compile()).AsQueryable());
        }

        [TearDown]
        public void TearDown()
        {
            subject.InvalidateTeamMetrics(testTeam);
        }

        private WorkItem AddItem(string referenceId, DateTime started, DateTime? closed)
        {
            var item = new WorkItem
            {
                Id = workItems.Count + 1,
                ReferenceId = referenceId,
                TeamId = testTeam.Id,
                StartedDate = started,
                ClosedDate = closed,
                StateCategory = closed.HasValue ? StateCategories.Done : StateCategories.Doing,
            };
            workItems.Add(item);
            return item;
        }

        [Test]
        public void GetWorkItemAgePercentiles_ItemClosedAfterTheSelectedDay_IsIncludedAndAgedToThatDay()
        {
            // Slice 03 domain example 1: started 01 Jul, closed 06 Jul, range ends 04 Jul -> age 4.
            AddItem("ITEM-1", started: Jul01, closed: Jul06);

            var percentiles = subject.GetWorkItemAgePercentilesForTeam(testTeam, Jul04).ToList();

            Assert.That(percentiles.Select(p => p.Value), Has.All.EqualTo(4));
        }

        [Test]
        public void GetWorkItemAgePercentiles_ItemClosedBeforeTheSelectedDay_IsExcluded()
        {
            // Slice 03 domain example 3: closed 06 Jul, range ends 10 Jul -> not in the population.
            AddItem("ITEM-CLOSED-EARLIER", started: Jul01, closed: Jul06);
            AddItem("ITEM-STILL-OPEN", started: Jul01, closed: null);

            var percentiles = subject.GetWorkItemAgePercentilesForTeam(testTeam, Jul10).ToList();

            // Only the still-open item contributes; its age on 10 Jul is 10.
            Assert.That(percentiles.Select(p => p.Value), Has.All.EqualTo(10));
        }

        [Test]
        public void GetWorkItemAgePercentiles_ItemClosedOnTheSelectedDayItself_IsExcluded()
        {
            // WasItemProgressOnDay treats an item as in progress only while ClosedDate > day, so an item
            // closing ON the selected day is NOT in the population. Slice-03 domain example 2 asserts the
            // opposite ("range ends 06 July -> included, age 6"); this test pins the SHIPPED primitive,
            // which DESIGN D14 forbids changing. Flagged upstream for the user to confirm.
            AddItem("ITEM-CLOSED-ON-DAY", started: Jul01, closed: Jul06);

            var percentiles = subject.GetWorkItemAgePercentilesForTeam(testTeam, Jul06).ToList();

            // BuildPercentiles always emits the four 50/70/85/95 entries; an empty population shows up
            // as zero VALUES, not as an empty list.
            Assert.That(percentiles.Select(p => p.Value), Has.All.Zero);
        }

        [Test]
        public void GetWorkItemAgePercentiles_ItemStartedAfterTheSelectedDay_IsExcluded()
        {
            AddItem("ITEM-FUTURE", started: Jul10, closed: null);

            var percentiles = subject.GetWorkItemAgePercentilesForTeam(testTeam, Jul04).ToList();

            Assert.That(percentiles.Select(p => p.Value), Has.All.Zero);
        }

        [Test]
        public void GetWorkItemAgePercentiles_AgreesWithTheTotalWorkItemAgeReferenceAtTheSameDate()
        {
            // CI6: the percentile projection and GetTotalWorkItemAge both claim to describe "the ages of
            // the items in progress on endDate". GetTotalWorkItemAge already goes through
            // GenerateTotalWorkItemAgeByDay and is already correct, so their SUM must agree. If they ever
            // diverge, a second definition of "age on a day" has been introduced.
            AddItem("A", started: Jul01, closed: Jul06);
            AddItem("B", started: Jul01, closed: null);
            AddItem("C", started: Jul04, closed: null);

            var ages = subject.GetWipSnapshotForTeam(testTeam, Jul04)
                .Select(i => i.AgeOnDay(Jul04))
                .Where(age => age > 0)
                .ToList();
            var referenceTotal = subject.GetTotalWorkItemAge(testTeam, Jul04);

            Assert.That(ages.Sum(), Is.EqualTo(referenceTotal));
        }

        [Test]
        public void GetWorkItemAgePercentiles_MatchHandComputedAgesOnTheSelectedDay()
        {
            // CI6, second oracle. Added 2026-07-19 by the DISTILL review gate.
            //
            // The agreement test above compares the new projection against GenerateTotalWorkItemAgeByDay.
            // That is an anti-drift guard, NOT a correctness proof: if both definitions are wrong in the
            // same way they still agree and the test stays green. This test states the expected ages
            // outright, derived by hand from the D3 rule (age = (endDate − started) + 1, inclusive), so at
            // least one assertion in the suite is independent of the reference implementation.
            //
            //   A: started Jul01, closed Jul06 → in progress on Jul04 → 1,2,3,4  → age 4
            //   B: started Jul01, still open   → in progress on Jul04 → 1,2,3,4  → age 4
            //   C: started Jul04, still open   → in progress on Jul04 → 1        → age 1
            AddItem("A", started: Jul01, closed: Jul06);
            AddItem("B", started: Jul01, closed: null);
            AddItem("C", started: Jul04, closed: null);

            var ages = subject.GetWipSnapshotForTeam(testTeam, Jul04)
                .Select(i => i.AgeOnDay(Jul04))
                .OrderBy(age => age)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(ages, Is.EqualTo(new[] { 1, 4, 4 }));
                // Pins the total the agreement test compares against, so a drift in BOTH definitions
                // cannot pass unnoticed.
                Assert.That(ages.Sum(), Is.EqualTo(9));
            });
        }

        [Test]
        public void GetWorkItemAgePercentiles_WhenTheRangeEndsToday_MatchesTheTodayAnchoredProperty()
        {
            // CI4 / US-04 AC2: this is a correctness fix for HISTORICAL ranges only. With endDate = today
            // the population is the currently-Doing set and every age must equal the shipped WorkItemAge
            // property, byte for byte.
            var today = DateTime.UtcNow.Date;
            AddItem("OPEN-NOW", started: today.AddDays(-3), closed: null);
            AddItem("ALSO-OPEN-NOW", started: today.AddDays(-9), closed: null);

            var wip = subject.GetWipSnapshotForTeam(testTeam, today).ToList();
            var asOfAges = wip.Select(i => i.AgeOnDay(today)).OrderBy(a => a).ToList();
            var todayAnchoredAges = wip.Select(i => i.WorkItemAge).OrderBy(a => a).ToList();

            Assert.That(asOfAges, Is.EqualTo(todayAnchoredAges));
        }
    }

    /// <summary>
    /// Portfolio-scope mirror of <see cref="TeamWorkItemAgeAsOfDateTest"/>. CI2: a widget fixed for
    /// teams and left broken for portfolios is not done.
    /// </summary>
    [TestFixture]
    public class PortfolioWorkItemAgeAsOfDateTest
    {
        private Mock<IRepository<Feature>> featureRepository;
        private PortfolioMetricsService subject;
        private Portfolio portfolio;
        private List<Feature> features;

        private static readonly DateTime Jul01 = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul04 = new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Jul06 = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void Setup()
        {
            featureRepository = new Mock<IRepository<Feature>>();
            var appSettingService = new Mock<IAppSettingService>();
            appSettingService.Setup(m => m.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(Mock.Of<IForecastService>());

            portfolio = new Portfolio { Id = 1, Name = "Test Portfolio" };
            subject = new PortfolioMetricsService(
                Mock.Of<ILogger<PortfolioMetricsService>>(),
                featureRepository.Object,
                appSettingService.Object,
                serviceProvider.Object,
                Mock.Of<IFeatureStateTransitionRepository>());

            features = new List<Feature>();
            featureRepository
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns((Expression<Func<Feature, bool>> pred) => features.Where(pred.Compile()).AsQueryable());
        }

        [TearDown]
        public void TearDown()
        {
            subject.InvalidatePortfolioMetrics(portfolio);
        }

        private void AddFeature(string referenceId, DateTime started, DateTime? closed)
        {
            var feature = new Feature
            {
                Id = features.Count + 1,
                ReferenceId = referenceId,
                StartedDate = started,
                ClosedDate = closed,
                StateCategory = closed.HasValue ? StateCategories.Done : StateCategories.Doing,
            };
            feature.Portfolios.Add(portfolio);
            features.Add(feature);
        }

        [Test]
        public void GetWorkItemAgePercentiles_FeatureClosedAfterTheSelectedDay_IsIncludedAndAgedToThatDay()
        {
            AddFeature("F-1", started: Jul01, closed: Jul06);

            var percentiles = subject.GetWorkItemAgePercentilesForPortfolio(portfolio, Jul04).ToList();

            Assert.That(percentiles.Select(p => p.Value), Has.All.EqualTo(4));
        }

        [Test]
        public void GetWorkItemAgePercentiles_WhenTheRangeEndsToday_MatchesTheTodayAnchoredProperty()
        {
            var today = DateTime.UtcNow.Date;
            AddFeature("F-OPEN", started: today.AddDays(-5), closed: null);

            var wip = subject.GetInProgressFeaturesForPortfolio(portfolio, today).ToList();

            Assert.That(
                wip.Select(f => f.AgeOnDay(today)).OrderBy(a => a),
                Is.EqualTo(wip.Select(f => f.WorkItemAge).OrderBy(a => a)));
        }
    }
}
