using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class TeamMetricsServiceTests
    {
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Team testTeam;

        private TeamMetricsService subject;
        private List<WorkItem> workItems;

        [SetUp]
        public void Setup()
        {
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();

            var appSettingsServiceMock = new Mock<IAppSettingService>();
            appSettingsServiceMock.Setup(x => x.GetThroughputRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });

            testTeam = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30 };
            subject = new TeamMetricsService(Mock.Of<ILogger<TeamMetricsService>>(), workItemRepositoryMock.Object, featureRepositoryMock.Object, appSettingsServiceMock.Object);
            workItems = new List<WorkItem>();

            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
            .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());
        }

        [TearDown]
        public void TearDown()
        {
            subject.InvalidateTeamMetrics(testTeam);
        }

        [Test]
        public void GetFeaturesInProgressForTeam_NoWorkItemsInDoing_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.ToDo, 1, "Feature1");
            AddWorkItem(StateCategories.Done, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetFeaturesInProgressForTeam_NoWorkOfTeamInDoing_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.Doing, 2, "Feature1");

            var featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetFeaturesInProgressForTeam_MultipleItemsOfSameFeatureInProgress_ReturnsFeatureOnce()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");
            AddWorkItem(StateCategories.Doing, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetFeaturesInProgressForTeam_MultipleItemsOfDifferentFeatureInProgress_ReturnsFeatures()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");
            AddWorkItem(StateCategories.Doing, 1, "Feature2");

            var featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetFeaturesInProgressForTeam_CachesValue()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(1));

            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetFeaturesInProgressForTeam_InvalidateCache()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(1));
            subject.InvalidateTeamMetrics(testTeam);

            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            featuresInProgress = subject.GetCurrentFeaturesInProgressForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetCurrentWipForTeam_NoWorkItemsInDoing_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            var featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetCurrentWipForTeam_NoWorkOfTeamInDoing_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.Doing, 2, string.Empty);

            var featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(0));
        }

        [Test]
        public void GetCurrentWipForTeam_MultipleItemsInProgress_ReturnsAllItems()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");
            AddWorkItem(StateCategories.Doing, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetCurrentWipForTeam_MultipleItemsOfDifferentFeatureInProgress_ReturnsItems()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");
            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            AddWorkItem(StateCategories.Doing, 1, "Feature2");

            var featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(3));
        }

        [Test]
        public void GetCurrentWipForTeam_CachesValue()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(1));

            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(1));
        }

        [Test]
        public void GetCurrentWipForTeam_InvalidateCache()
        {
            AddWorkItem(StateCategories.Doing, 1, "Feature1");

            var featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(1));
            subject.InvalidateTeamMetrics(testTeam);

            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            featuresInProgress = subject.GetCurrentWipForTeam(testTeam).ToList();
            Assert.That(featuresInProgress, Has.Count.EqualTo(2));
        }

        [Test]
        public void GetCurrentThroughputForTeam_NoItemsDone_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.Doing, 1, string.Empty);
            AddWorkItem(StateCategories.Unknown, 1, string.Empty);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(0));
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemForOtherTeamDone_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.Done, 2, string.Empty);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(0));
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemsDone_ReturnsThroughputByDay()
        {
            testTeam.ThroughputHistory = 10;

            for (var i = 0; i < 10; i++)
            {
                var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
                workItem.ClosedDate = DateTime.UtcNow.AddDays(-i);
            }

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.Multiple(() =>
            {
                Assert.That(throughput.Total, Is.EqualTo(10));
                Assert.That(throughput.History, Is.EqualTo(10));

                foreach (var day in throughput.ValuePerUnitOfTime)
                {
                    Assert.That(day, Is.EqualTo(1));
                }
            });
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemClosedBeforeHistory_DoesNotIncludeInThroughput()
        {
            testTeam.ThroughputHistory = 10;

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(+1);

            // In Range
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-11);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(1));
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemClosedAfterHistory_DoesNotIncludeInThroughput()
        {
            testTeam.ThroughputHistory = 10;
            AddWorkItem(StateCategories.Done, 1, string.Empty);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(11);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(1));
        }

        [Test]
        public void GetCurrentThroughputForTeam_UseFixedThroughput_UsesSpecificTimeRange()
        {
            testTeam.UseFixedDatesForThroughput = true;
            testTeam.ThroughputHistoryStartDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            testTeam.ThroughputHistoryEndDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 9, 0, 0, 0, DateTimeKind.Utc);

            // In Range
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(3));
        }

        [Test]
        public void GetThroughputForTeam_GivenStartAndEndDate_ReturnsThroughputFromThisRange()
        {
            testTeam.UseFixedDatesForThroughput = false;
            testTeam.ThroughputHistory = 7;

            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 9, 0, 0, 0, DateTimeKind.Utc);

            // In Range
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            var throughput = subject.GetThroughputForTeam(testTeam, startDate, endDate);

            Assert.That(throughput.Total, Is.EqualTo(3));
        }

        [Test]
        public void GetCurrentThroughputForTeam_CachesValue()
        {
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);
            Assert.That(throughput.Total, Is.EqualTo(1));

            AddWorkItem(StateCategories.Done, 1, string.Empty);
            throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(1));
        }

        [Test]
        public void GetCurrentThroughputForTeam_InvalidateCache()
        {
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);
            Assert.That(throughput.Total, Is.EqualTo(1));

            AddWorkItem(StateCategories.Done, 1, string.Empty);
            subject.InvalidateTeamMetrics(testTeam);

            throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(2));
        }

        [Test]
        public void GetThroughputForTeam_DoesNotCacheValue()
        {
            AddWorkItem(StateCategories.Done, 1, string.Empty);
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;

            var throughput = subject.GetThroughputForTeam(testTeam, startDate, endDate);
            Assert.That(throughput.Total, Is.EqualTo(1));

            AddWorkItem(StateCategories.Done, 1, string.Empty);
            throughput = subject.GetThroughputForTeam(testTeam, startDate, endDate);

            Assert.That(throughput.Total, Is.EqualTo(2));
        }

        [Test]
        public void GetWorkInProgressForTeam_ReturnsWorkInProgressPerDay()
        {
            var startDate = DateTime.UtcNow.AddDays(-9);
            var endDate = DateTime.UtcNow;

            // Add items that are all in progress, and that are closed one after the other
            for (var index = 0; index < 10; index++)
            {
                var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
                workItem.StartedDate = startDate.AddDays(-1);
                workItem.ClosedDate = startDate.AddDays(index);
            }

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(wipData.History, Is.EqualTo(10));

                for (var index = 0; index < 10 ; index++)
                {
                    Assert.That(wipData.ValuePerUnitOfTime[index], Is.EqualTo(9 - index));
                }
            });
        }

        [Test]
        public void GetWorkInProgressForTeam_ItemClosedBeforeStartDate_DoesNotCount()
        {
            var startDate = DateTime.UtcNow.AddDays(-9);
            var endDate = DateTime.UtcNow;
            
            var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            workItem.StartedDate = startDate.AddDays(-1);
            workItem.ClosedDate = startDate.AddDays(-1);

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.That(wipData.ValuePerUnitOfTime.Sum(), Is.EqualTo(0));
        }

        [Test]
        public void GetWorkInProgressOverTime_NoWorkItemsInDoing_ReturnsEmpty()
        {
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.That(wipData.ValuePerUnitOfTime.Sum(), Is.EqualTo(0));
        }

        [Test]
        public void GetWorkInProgressOverTime_NoWorkOfTeamInDoing_ReturnsEmpty()
        {
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            AddWorkItem(StateCategories.Doing, 1, string.Empty);

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.That(wipData.ValuePerUnitOfTime.Sum(), Is.EqualTo(0));
        }

        [Test]
        public void GetWorkInProgressOverTime_ItemClosed_DoesNotAddItemToDayWhenItWasClosed()
        {
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;

            var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            workItem.StartedDate = startDate;
            workItem.ClosedDate = endDate;

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(wipData.ValuePerUnitOfTime, Has.Length.EqualTo(2));
                Assert.That(wipData.ValuePerUnitOfTime[0], Is.EqualTo(1));
                Assert.That(wipData.ValuePerUnitOfTime[1], Is.EqualTo(0));
            });
        }

        [Test]
        public void GetCycleTimePercentilesForTeam_GetsCycleTimeForItemsInRange()
        {
            // Set up work item cycle times (1, 2, 3, ... 10)
            for (var index = 0; index < 10; index++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty);
                workItems[index].ClosedDate = DateTime.UtcNow.AddDays(-index);
                workItems[index].StartedDate = workItems[index].ClosedDate?.AddDays(-index);
            }

            var cycleTimePercentiles = subject.GetCycleTimePercentilesForTeam(testTeam, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(cycleTimePercentiles, Has.Count.EqualTo(4));

                Assert.That(cycleTimePercentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(cycleTimePercentiles[0].Value, Is.EqualTo(5));

                Assert.That(cycleTimePercentiles[1].Percentile, Is.EqualTo(70));
                Assert.That(cycleTimePercentiles[1].Value, Is.EqualTo(7));

                Assert.That(cycleTimePercentiles[2].Percentile, Is.EqualTo(85));
                Assert.That(cycleTimePercentiles[2].Value, Is.EqualTo(8));

                Assert.That(cycleTimePercentiles[3].Percentile, Is.EqualTo(95));
                Assert.That(cycleTimePercentiles[3].Value, Is.EqualTo(9));
            });
        }

        [Test]
        public void GetCycleTimePercentilesForTeam_NoClosedItems_ReturnsZeros()
        {
            var cycleTimePercentiles = subject.GetCycleTimePercentilesForTeam(testTeam, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(cycleTimePercentiles, Has.Count.EqualTo(4));

                Assert.That(cycleTimePercentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(cycleTimePercentiles[0].Value, Is.EqualTo(0));

                Assert.That(cycleTimePercentiles[1].Percentile, Is.EqualTo(70));
                Assert.That(cycleTimePercentiles[1].Value, Is.EqualTo(0));

                Assert.That(cycleTimePercentiles[2].Percentile, Is.EqualTo(85));
                Assert.That(cycleTimePercentiles[2].Value, Is.EqualTo(0));

                Assert.That(cycleTimePercentiles[3].Percentile, Is.EqualTo(95));
                Assert.That(cycleTimePercentiles[3].Value, Is.EqualTo(0));
            });
        }

        [Test]
        public void GetClosedItemsForTeam_ReturnsAllClosedItemsInTimeRange()
        {
            // Set up work item cycle times (1, 2, 3, ... 10)
            for (var index = 0; index < 10; index++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty);
                workItems[index].ClosedDate = DateTime.UtcNow.AddDays(-index);
                workItems[index].StartedDate = workItems[index].ClosedDate?.AddDays(-index);
            }

            var closedItemsInRange = subject.GetClosedItemsForTeam(testTeam, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(closedItemsInRange, Has.Count.EqualTo(10));

                for (var index = 0; index < 10; index++)
                {
                    Assert.That(closedItemsInRange[index].CycleTime, Is.EqualTo(index + 1));
                }
            });
        }

        [Test]
        public void GetClosedItemsForTeam_IgnoresItemsOutOfDateRange()
        {
            // Set up work item cycle times (1, 2, 3, ... 20)
            for (var index = 0; index < 20; index++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty);
                workItems[index].ClosedDate = DateTime.UtcNow.AddDays(-index);
                workItems[index].StartedDate = workItems[index].ClosedDate?.AddDays(-index);
            }

            var closedItemsInRange = subject.GetClosedItemsForTeam(testTeam, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(closedItemsInRange, Has.Count.EqualTo(10));

                for (var index = 0; index < 10; index++)
                {
                    Assert.That(closedItemsInRange[index].CycleTime, Is.EqualTo(index + 1));
                }
            });
        }

        private WorkItem AddWorkItem(StateCategories stateCategory, int teamId, string parentReference)
        {
            var workItem = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = stateCategory,
                TeamId = teamId,
                ParentReferenceId = parentReference,
                ClosedDate = DateTime.UtcNow
            };

            workItems.Add(workItem);

            return workItem;
        }
    }
}