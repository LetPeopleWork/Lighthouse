using System.Linq.Expressions;
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
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<IWorkItemStateTransitionRepository> stateTransitionRepositoryMock;
        private List<WorkItemStateTransition> stateTransitions;

        private Team testTeam;
        private TeamMetricsService subject;
        private List<WorkItem> workItems;
        private List<Feature> features;

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
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
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
            forecastFilterRuleServiceMock.Setup(s => s.GetEffectiveRuleSet(It.IsAny<Team>())).Returns((Lighthouse.Backend.Models.WorkItemRules.WorkItemRuleSet?)null);

            testTeam = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30 };

            stateTransitions = new List<WorkItemStateTransition>();
            stateTransitionRepositoryMock = new Mock<IWorkItemStateTransitionRepository>();
            stateTransitionRepositoryMock
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItemStateTransition, bool>>>()))
                .Returns((Expression<Func<WorkItemStateTransition, bool>> pred) =>
                    stateTransitions.Where(pred.Compile()).AsQueryable());

            subject = new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepositoryMock.Object,
                featureRepositoryMock.Object,
                appSettingsServiceMock.Object,
                serviceProvider.Object,
                blackoutPeriodServiceMock.Object,
                forecastFilterRuleServiceMock.Object,
                stateTransitionRepositoryMock.Object);

            workItems = new List<WorkItem>();
            features = new List<Feature>();
            workItemRepositoryMock
                .Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> pred) =>
                    workItems.Where(pred.Compile()).AsQueryable());
            featureRepositoryMock
                .Setup(x => x.GetByPredicate(It.IsAny<Func<Feature, bool>>()))
                .Returns((Func<Feature, bool> pred) =>
                    features.FirstOrDefault(pred));
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
                Id = 1,
                StateCategory = StateCategories.Doing,
                TeamId = 99,
                StartedDate = Day1
            };
            workItems.Add(item);

            var result = subject.GetWipSnapshotForTeam(testTeam, Day19);

            Assert.That(result, Is.Empty);
        }

        // ── GetWipSnapshotForTeam: as-of state projection (UPSTREAM-7) ────────

        [Test]
        public void GetWipSnapshotForTeam_ItemMovedOnSinceEndDate_ReportsStateItHadOnEndDate()
        {
            // The failing case from the field: in progress on Apr 19, Closed by the time we look.
            // The aging chart buckets by state, so reporting "Closed" drops it out of the chart
            // entirely while WIP-over-time still counts it.
            AddDoneItem(startedDate: Day1, closedDate: DateTime.UtcNow.Date);
            workItems[0].State = "Closed";
            AddTransition(workItemId: 1, toState: "Active", at: Day1);
            AddTransition(workItemId: 1, toState: "Closed", at: DateTime.UtcNow.Date);

            var item = subject.GetWipSnapshotForTeam(testTeam, Day19).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("Active"));
                Assert.That(item.StateCategory, Is.EqualTo(StateCategories.Doing));
            }
        }

        [Test]
        public void GetWipSnapshotForTeam_SeveralTransitionsBeforeEndDate_ReportsTheLastOne()
        {
            AddDoingItem(startedDate: Day1);
            AddTransition(workItemId: 1, toState: "Active", at: Day1);
            AddTransition(workItemId: 1, toState: "Resolved", at: Day10);
            AddTransition(workItemId: 1, toState: "Active", at: Day14);

            var item = subject.GetWipSnapshotForTeam(testTeam, Day19).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("Active"));
                Assert.That(item.CurrentStateEnteredAt, Is.EqualTo(Day14),
                    "time-in-state must be measured from the entry that was current on the end date");
            }
        }

        [Test]
        public void GetWipSnapshotForTeam_TransitionAfterEndDate_IsIgnored()
        {
            AddDoingItem(startedDate: Day1);
            AddTransition(workItemId: 1, toState: "Active", at: Day5);
            AddTransition(workItemId: 1, toState: "Resolved", at: Day19.AddDays(1));

            var item = subject.GetWipSnapshotForTeam(testTeam, Day19).Single();

            Assert.That(item.State, Is.EqualTo("Active"));
        }

        [Test]
        public void GetWipSnapshotForTeam_TransitionLaterOnEndDate_StillCounts()
        {
            // Day granularity, matching WIP-over-time: a transition at 13:20 on the end date is part
            // of that day, so the item's state on that day is the one it ended the day in.
            AddDoingItem(startedDate: Day1);
            AddTransition(workItemId: 1, toState: "Active", at: Day5);
            AddTransition(workItemId: 1, toState: "Resolved", at: Day19.AddHours(13));

            var item = subject.GetWipSnapshotForTeam(testTeam, Day19).Single();

            Assert.That(item.State, Is.EqualTo("Resolved"));
        }

        [Test]
        public void GetWipSnapshotForTeam_NoTransitionHistory_KeepsCurrentState()
        {
            // Items synced before state history was captured. Falling back to the current state is
            // exactly what happened before this projection existed, so nothing can regress.
            AddDoingItem(startedDate: Day1);
            workItems[0].State = "In Progress";
            workItems[0].CurrentStateEnteredAt = Day10;

            var item = subject.GetWipSnapshotForTeam(testTeam, Day19).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.State, Is.EqualTo("In Progress"));
                Assert.That(item.CurrentStateEnteredAt, Is.EqualTo(Day10));
            }
        }

        [Test]
        public void GetWipSnapshotForTeam_AllTransitionsAfterEndDate_KeepsCurrentState()
        {
            AddDoingItem(startedDate: Day1);
            workItems[0].State = "In Progress";
            AddTransition(workItemId: 1, toState: "Resolved", at: Day19.AddDays(1));

            var item = subject.GetWipSnapshotForTeam(testTeam, Day19).Single();

            Assert.That(item.State, Is.EqualTo("In Progress"));
        }

        [Test]
        public void GetWipSnapshotForTeam_OtherItemsTransitions_DoNotLeakAcross()
        {
            AddDoingItem(startedDate: Day1);
            AddDoingItem(startedDate: Day1);
            workItems[1].State = "In Progress";
            AddTransition(workItemId: 1, toState: "Resolved", at: Day10);

            var items = subject.GetWipSnapshotForTeam(testTeam, Day19).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items.Single(i => i.Id == 1).State, Is.EqualTo("Resolved"));
                Assert.That(items.Single(i => i.Id == 2).State, Is.EqualTo("In Progress"));
            }
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
        public void GetThroughputInfoForTeam_NoPreviousPeriodData_ReturnsUp()
        {
            // Current period has data, previous has none
            AddDoneItem(closedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetThroughputInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
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
        public void GetArrivalsInfoForTeam_NoPreviousPeriodData_ReturnsUp()
        {
            AddDoingItem(startedDate: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));

            var result = subject.GetArrivalsInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
        }

        [Test]
        public void GetWipOverviewInfoForTeam_NoPreviousSnapshot_ReturnsUp()
        {
            AddDoingItem(startedDate: Day10);

            var result = subject.GetWipOverviewInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
        }

        [Test]
        public void GetFeaturesWorkedOnInfoForTeam_NoPreviousSnapshot_ReturnsUp()
        {
            AddFeature("FEATURE-1");
            AddDoingItem(startedDate: Day10, parentReferenceId: "FEATURE-1");

            var result = subject.GetFeaturesWorkedOnInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
        }

        [Test]
        public void GetTotalWorkItemAgeInfoForTeam_NoPreviousSnapshot_ReturnsUp()
        {
            AddDoingItem(startedDate: Day10);

            var result = subject.GetTotalWorkItemAgeInfoForTeam(testTeam, Day5, Day14);

            Assert.That(result.Comparison.Direction, Is.EqualTo("up"));
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

        private void AddDoingItem(DateTime? startedDate = null, string? parentReferenceId = null)
        {
            var item = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = StateCategories.Doing,
                TeamId = testTeam.Id,
                StartedDate = startedDate ?? Day1,
                ParentReferenceId = parentReferenceId,
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

        private void AddTransition(int workItemId, string toState, DateTime at)
        {
            stateTransitions.Add(new WorkItemStateTransition
            {
                Id = stateTransitions.Count + 1,
                WorkItemId = workItemId,
                ToState = toState,
                TransitionedAt = at,
            });
        }

        private void AddFeature(string referenceId)
        {
            features.Add(new Feature
            {
                Id = features.Count + 1,
                ReferenceId = referenceId,
                Name = referenceId,
                StateCategory = StateCategories.Doing,
                Type = string.Empty,
                State = string.Empty,
                Url = string.Empty,
                Order = string.Empty,
            });
        }
    }
}
