using System.Linq.Expressions;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Story 4804 — date-aware snapshot and comparison methods added to TeamMetricsService.
    /// </summary>
    [TestFixture]
    public class TeamMetricsServiceSnapshotTests
    {
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IRepository<BlackoutPeriod>> blackoutPeriodRepositoryMock;

        private Team testTeam;
        private TeamMetricsService subject;
        private List<WorkItem> workItems;

        private static readonly DateTime Day1 = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Day5 = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Day10 = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Day14 = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Day19 = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void Setup()
        {
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            blackoutPeriodRepositoryMock = new Mock<IRepository<BlackoutPeriod>>();
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(Enumerable.Empty<BlackoutPeriod>().AsQueryable());

            var appSettingsServiceMock = new Mock<IAppSettingService>();
            appSettingsServiceMock.Setup(x => x.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(Mock.Of<IForecastService>());

            testTeam = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30 };
            subject = new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepositoryMock.Object,
                featureRepositoryMock.Object,
                appSettingsServiceMock.Object,
                serviceProvider.Object,
                blackoutPeriodRepositoryMock.Object);

            workItems = new List<WorkItem>();
            workItemRepositoryMock
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> pred) =>
                    workItems.Where(pred.Compile()).AsQueryable());
        }

        [TearDown]
        public void TearDown()
        {
            subject.InvalidateTeamMetrics(testTeam);
        }

        // ── GetWipSnapshotForTeam ─────────────────────────────────────────────

        [Test]
        public void GetWipSnapshotForTeam_ItemActiveOnEndDate_ReturnsItem()
        {
            // Started Apr 1, NOT closed → in progress on Apr 19
            AddDoingItem(startedDate: Day1);

            var result = subject.GetWipSnapshotForTeam(testTeam, Day19);

            Assert.That(result.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetWipSnapshotForTeam_ItemClosedBeforeEndDate_NotReturned()
        {
            // Started Apr 1, closed Apr 14 → NOT in progress on Apr 19
            AddDoneItem(startedDate: Day1, closedDate: Day14);

            var result = subject.GetWipSnapshotForTeam(testTeam, Day19);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetWipSnapshotForTeam_ItemClosedOnEndDate_NotReturned()
        {
            // WasItemProgressOnDay: closedDate must be AFTER the day → closed on endDate means NOT included
            AddDoneItem(startedDate: Day1, closedDate: Day19);

            var result = subject.GetWipSnapshotForTeam(testTeam, Day19);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetWipSnapshotForTeam_ItemNotYetStartedOnEndDate_NotReturned()
        {
            // Started Apr 19, closed later → was NOT started on Apr 5
            AddDoneItem(startedDate: Day19, closedDate: Day19);

            var result = subject.GetWipSnapshotForTeam(testTeam, Day5);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetWipSnapshotForTeam_MixedItems_ReturnsOnlyActiveOnEndDate()
        {
            AddDoingItem(startedDate: Day1);               // still open → included
            AddDoneItem(startedDate: Day1, closedDate: Day14); // closed before → excluded
            AddDoneItem(startedDate: Day1, closedDate: Day19); // closed on day → excluded
            AddDoingItem(startedDate: Day19);              // started on day → included (startedDate <= endDate)

            var result = subject.GetWipSnapshotForTeam(testTeam, Day19);

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public void GetWipSnapshotForTeam_WrongTeam_NotReturned()
        {
            var item = new WorkItem
            {
                Id = 1, StateCategory = StateCategories.Doing, TeamId = 99,
                StartedDate = Day1
            };
            workItems.Add(item);

            var result = subject.GetWipSnapshotForTeam(testTeam, Day19);

            Assert.That(result, Is.Empty);
        }

        // ── GetTotalWorkItemAge (date-aware overload) ─────────────────────────

        [Test]
        public void GetTotalWorkItemAge_DateAware_SingleItem_ReturnsDaysActiveOnEndDate()
        {
            // Started Apr 5, not closed → age on Apr 19 = (19 - 5) + 1 = 15 days
            AddDoingItem(startedDate: Day5);

            var result = subject.GetTotalWorkItemAge(testTeam, Day19);

            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void GetTotalWorkItemAge_DateAware_ItemClosedBeforeEndDate_NotCounted()
        {
            AddDoneItem(startedDate: Day1, closedDate: Day10);

            var result = subject.GetTotalWorkItemAge(testTeam, Day19);

            Assert.That(result, Is.Zero);
        }

        [Test]
        public void GetTotalWorkItemAge_DateAware_MultipleItems_SumsAges()
        {
            // Item A: started Apr 1, age on Apr 19 = (19-1)+1 = 19
            // Item B: started Apr 5, age on Apr 19 = (19-5)+1 = 15
            AddDoingItem(startedDate: Day1);
            AddDoingItem(startedDate: Day5);

            var result = subject.GetTotalWorkItemAge(testTeam, Day19);

            Assert.That(result, Is.EqualTo(34));
        }

        [Test]
        public void GetTotalWorkItemAge_DateAware_NoItemsInProgress_ReturnsZero()
        {
            var result = subject.GetTotalWorkItemAge(testTeam, Day19);

            Assert.That(result, Is.Zero);
        }

        // ── GetThroughputInfoForTeam ──────────────────────────────────────────

        [Test]
        public void GetThroughputInfoForTeam_CurrentHigherThanPrevious_ReturnsUp()
        {
            // Current period: Apr 5-14 (10 days) → 4 items closed
            // Previous period: Mar 26-Apr 4 (10 days) → 2 items closed
            AddDoneItem(closedDate: new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
                Assert.That(result.Total, Is.EqualTo(4));
            }
        }

        [Test]
        public void GetThroughputInfoForTeam_CurrentLowerThanPrevious_ReturnsDown()
        {
            // Current period: Apr 5-14 → 1 item
            // Previous period: Mar 26-Apr 4 → 4 items
            AddDoneItem(closedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("down"));
        }

        [Test]
        public void GetThroughputInfoForTeam_CurrentEqualsAtDisplayPrecision_ReturnsFlat()
        {
            // Both periods: 3 items → totals equal → flat
            AddDoneItem(closedDate: new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("flat"));
        }

        [Test]
        public void GetThroughputInfoForTeam_NoPreviousPeriodData_ReturnsNone()
        {
            // Current period has data, previous has none
            AddDoneItem(closedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("none"));
        }

        [Test]
        public void GetThroughputInfoForTeam_IncludesDailyAverage()
        {
            // 4 items in 10 days → 0.4/day
            AddDoneItem(closedDate: new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 8, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc)); // previous period

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.DailyAverage, Is.EqualTo(0.4).Within(0.01));
        }

        [Test]
        public void GetThroughputInfoForTeam_IncludesDateRangeLabels()
        {
            AddDoneItem(closedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
            AddDoneItem(closedDate: new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Comparison.CurrentLabel, Does.Contain("2026-04-05"));
                Assert.That(result.Comparison.CurrentLabel, Does.Contain("2026-04-14"));
                Assert.That(result.Comparison.PreviousLabel, Does.Contain("2026-03-26"));
                Assert.That(result.Comparison.PreviousLabel, Does.Contain("2026-04-04"));
            }
        }

        // ── GetArrivalsInfoForTeam ─────────────────────────────────────────────

        [Test]
        public void GetArrivalsInfoForTeam_CurrentHigherThanPrevious_ReturnsUp()
        {
            // Current period: Apr 5-14 → 3 arrivals
            // Previous period: Mar 26-Apr 4 → 1 arrival
            AddDoingItem(startedDate: new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc));
            AddDoingItem(startedDate: new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc));
            AddDoingItem(startedDate: new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc));
            AddDoingItem(startedDate: new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetArrivalsInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
        }

        [Test]
        public void GetArrivalsInfoForTeam_NoPreviousPeriodData_ReturnsNone()
        {
            AddDoingItem(startedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetArrivalsInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("none"));
        }

        [Test]
        public void GetArrivalsInfoForTeam_IncludesDailyAverage()
        {
            // 2 arrivals in 10 days → 0.2/day
            AddDoingItem(startedDate: new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc));
            AddDoingItem(startedDate: new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc));
            AddDoingItem(startedDate: new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc)); // previous period

            var result = subject.GetArrivalsInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.DailyAverage, Is.EqualTo(0.2).Within(0.01));
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private void AddDoingItem(DateTime? startedDate = null)
        {
            var item = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = StateCategories.Doing,
                TeamId = testTeam.Id,
                StartedDate = startedDate ?? Day1,
            };
            workItems.Add(item);
        }

        private void AddDoneItem(DateTime? startedDate = null, DateTime? closedDate = null)
        {
            var item = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = StateCategories.Done,
                TeamId = testTeam.Id,
                StartedDate = startedDate ?? Day1,
                ClosedDate = closedDate ?? DateTime.UtcNow,
            };
            workItems.Add(item);
        }
    }
}
