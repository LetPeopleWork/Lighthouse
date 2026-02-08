using System.Linq.Expressions;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class TeamMetricsServiceTests
    {
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IForecastService> forecastServiceMock;

        private Team testTeam;

        private TeamMetricsService subject;
        private List<WorkItem> workItems;

        [SetUp]
        public void Setup()
        {
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            forecastServiceMock = new Mock<IForecastService>();

            var appSettingsServiceMock = new Mock<IAppSettingService>();
            appSettingsServiceMock.Setup(x => x.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });
            
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(forecastServiceMock.Object);

            testTeam = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30 };
            subject = new TeamMetricsService(Mock.Of<ILogger<TeamMetricsService>>(), workItemRepositoryMock.Object, featureRepositoryMock.Object, appSettingsServiceMock.Object, serviceProvider.Object);

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

            Assert.That(throughput.Total, Is.Zero);
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemForOtherTeamDone_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.Done, 2, string.Empty);

            var throughput = subject.GetCurrentThroughputForTeam(testTeam);

            Assert.That(throughput.Total, Is.Zero);
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.Total, Is.EqualTo(10));
                Assert.That(throughput.History, Is.EqualTo(10));

                foreach (var kvp in throughput.WorkItemsPerUnitOfTime)
                {
                    Assert.That(kvp.Value, Has.Count.EqualTo(1));
                }
            }
            ;
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
        public void GetStartedItemsForTeam_GivenStartDate_ReturnsStartedItemsPerDayFromThisRange()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).StartedDate = new DateTime(1991, 4, 9, 0, 0, 0, DateTimeKind.Utc);

            // In Range
            AddWorkItem(StateCategories.Doing, 1, string.Empty).StartedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).StartedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Doing, 1, string.Empty).StartedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).StartedDate = new DateTime(1991, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            var throughput = subject.GetStartedItemsForTeam(testTeam, startDate, endDate);

            Assert.That(throughput.Total, Is.EqualTo(3));
        }

        [Test]
        public void GetCreatedItemsRunChartForTeam_GivenStartAndEndDate_ReturnsCreatedItemsFromThisRange()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 9, 0, 0, 0, DateTimeKind.Utc);

            // In Range
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            var bugItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            bugItem.CreatedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            bugItem.Type = "Bug";

            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            var createdItems = subject.GetCreatedItemsForTeam(testTeam, ["User Story", "Bug"], startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(createdItems.Total, Is.EqualTo(3));
                Assert.That(createdItems.History, Is.EqualTo(8));

                var workItemsPerUnitOfTime = createdItems.WorkItemsPerUnitOfTime;
                Assert.That(workItemsPerUnitOfTime.Keys.Select(k => workItemsPerUnitOfTime[k].Count).ToArray(), Is.EqualTo([1, 0, 0, 0, 1, 0, 0, 1]));
            }
            ;
        }

        [Test]
        [TestCase("User Story")]
        [TestCase("USER STORY")]
        [TestCase("user story")]
        [TestCase("UsEr StOrY")]
        public void GetCreatedItemsRunChartForTeam_GivenWorkItemTypeFilter_ReturnsOnlyItemsOfType(string includedWorkItemType)
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 9, 0, 0, 0, DateTimeKind.Utc);

            // In Range
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            var bugItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            bugItem.CreatedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            bugItem.Type = "Bug";

            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            var createdItems = subject.GetCreatedItemsForTeam(testTeam, [includedWorkItemType], startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(createdItems.Total, Is.EqualTo(2));
                Assert.That(createdItems.History, Is.EqualTo(8));

                var workItemsPerUnitOfTime = createdItems.WorkItemsPerUnitOfTime;
                Assert.That(workItemsPerUnitOfTime.Keys.Select(k => workItemsPerUnitOfTime[k].Count).ToArray(), Is.EqualTo([1, 0, 0, 0, 0, 0, 0, 1]));
            }
            ;
        }



        [Test]
        public void GetCreatedItemsRunChartForTeam_NoMatchingItems_ReturnsEmpty()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 4, 9, 0, 0, 0, DateTimeKind.Utc);

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).CreatedDate = new DateTime(1991, 3, 31, 0, 0, 0, DateTimeKind.Utc);

            var createdItems = subject.GetCreatedItemsForTeam(testTeam, ["User Story", "Bug"], startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(createdItems.Total, Is.Zero);
                Assert.That(createdItems.History, Is.EqualTo(8));

                var workItemsPerUnitOfTime = createdItems.WorkItemsPerUnitOfTime;
                Assert.That(workItemsPerUnitOfTime.Keys.Select(k => workItemsPerUnitOfTime[k].Count).ToArray(), Is.EqualTo([0, 0, 0, 0, 0, 0, 0, 0]));
            }
            ;
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wipData.History, Is.EqualTo(10));

                for (var index = 0; index < 10; index++)
                {
                    Assert.That(wipData.WorkItemsPerUnitOfTime[index], Has.Count.EqualTo(9 - index));
                }
            }
            ;
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

            Assert.That(wipData.Total, Is.Zero);
        }

        [Test]
        public void GetWorkInProgressOverTime_NoWorkItemsInDoing_ReturnsEmpty()
        {
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.That(wipData.Total, Is.Zero);
        }

        [Test]
        public void GetWorkInProgressOverTime_NoWorkOfTeamInDoing_ReturnsEmpty()
        {
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            AddWorkItem(StateCategories.Doing, 1, string.Empty);

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.That(wipData.Total, Is.Zero);
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wipData.WorkItemsPerUnitOfTime, Has.Count.EqualTo(2));
                Assert.That(wipData.WorkItemsPerUnitOfTime[0], Has.Count.EqualTo(1));
                Assert.That(wipData.WorkItemsPerUnitOfTime[1], Is.Empty);
            }
            ;
        }

        [Test]
        public void GetWorkInProgressOverTime_ItemNotClosed_StartedAfterDateRange_DoesNotAddItemToDaysBeforeItWasStarted()
        {
            var startDate = DateTime.UtcNow.AddDays(-2);
            var endDate = DateTime.UtcNow;

            var workItem = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            workItem.StartedDate = DateTime.UtcNow.AddDays(-1);
            workItem.ClosedDate = null;

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wipData.WorkItemsPerUnitOfTime, Has.Count.EqualTo(3));
                Assert.That(wipData.WorkItemsPerUnitOfTime[0], Has.Count.EqualTo(0));
                Assert.That(wipData.WorkItemsPerUnitOfTime[1], Has.Count.EqualTo(1));
                Assert.That(wipData.WorkItemsPerUnitOfTime[2], Has.Count.EqualTo(1));
            }
            ;
        }

        [Test]
        public void GetWorkInProgressOverTime_ItemInDoneState_NoClosedDateSet_AssumeItWasClosedOnSameDay()
        {
            var startDate = DateTime.UtcNow.AddDays(-2);
            var endDate = DateTime.UtcNow;

            var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            workItem.StartedDate = DateTime.UtcNow.AddDays(-1);
            workItem.ClosedDate = null;

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wipData.WorkItemsPerUnitOfTime, Has.Count.EqualTo(3));
                Assert.That(wipData.WorkItemsPerUnitOfTime[0], Has.Count.EqualTo(0));
                Assert.That(wipData.WorkItemsPerUnitOfTime[1], Has.Count.EqualTo(0));
                Assert.That(wipData.WorkItemsPerUnitOfTime[2], Has.Count.EqualTo(0));
            }
            ;
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

            using (Assert.EnterMultipleScope())
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
            }
            ;
        }

        [Test]
        public void GetCycleTimePercentilesForTeam_NoClosedItems_ReturnsZeros()
        {
            var cycleTimePercentiles = subject.GetCycleTimePercentilesForTeam(testTeam, DateTime.UtcNow.AddDays(-10), DateTime.UtcNow).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cycleTimePercentiles, Has.Count.EqualTo(4));

                Assert.That(cycleTimePercentiles[0].Percentile, Is.EqualTo(50));
                Assert.That(cycleTimePercentiles[0].Value, Is.Zero);

                Assert.That(cycleTimePercentiles[1].Percentile, Is.EqualTo(70));
                Assert.That(cycleTimePercentiles[1].Value, Is.Zero);

                Assert.That(cycleTimePercentiles[2].Percentile, Is.EqualTo(85));
                Assert.That(cycleTimePercentiles[2].Value, Is.Zero);

                Assert.That(cycleTimePercentiles[3].Percentile, Is.EqualTo(95));
                Assert.That(cycleTimePercentiles[3].Value, Is.Zero);
            }
            ;
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedItemsInRange, Has.Count.EqualTo(10));

                for (var index = 0; index < 10; index++)
                {
                    Assert.That(closedItemsInRange[index].CycleTime, Is.EqualTo(index + 1));
                }
            }
            ;
        }

        [Test]
        public void GetClosedItemsForTeam_ItemClosedAtEndDate_ReturnsItem()
        {
            var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            workItem.StartedDate = DateTime.UtcNow.AddDays(-1);
            workItem.ClosedDate = DateTime.UtcNow;

            var closedItemsInRange = subject.GetClosedItemsForTeam(testTeam, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedItemsInRange, Has.Count.EqualTo(1));
                Assert.That(closedItemsInRange[0].CycleTime, Is.EqualTo(2));
            }
            ;
        }

        [Test]
        public void GetClosedItemsForTeam_ItemClosedAtStartDate_ReturnsItem()
        {
            var workItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            workItem.StartedDate = DateTime.Now.AddDays(-1);
            workItem.ClosedDate = DateTime.Now.AddDays(-1);

            var closedItemsInRange = subject.GetClosedItemsForTeam(testTeam, DateTime.Today.AddDays(-1), DateTime.Today).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedItemsInRange, Has.Count.EqualTo(1));
                Assert.That(closedItemsInRange[0].CycleTime, Is.EqualTo(1));
            }
            ;
        }

        [Test]
        public void GetClosedItemsForTeam_IgnoresItemsOutOfDateRange()
        {
            // Set up work item cycle times (1, 2, 3, ... 20)
            for (var index = 0; index < 20; index++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty);
                workItems[index].ClosedDate = DateTime.Now.AddDays(-index);
                workItems[index].StartedDate = workItems[index].ClosedDate?.AddDays(-index);
            }

            var closedItemsInRange = subject.GetClosedItemsForTeam(testTeam, DateTime.Now.AddDays(-10), DateTime.Now).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closedItemsInRange, Has.Count.EqualTo(11));

                for (var index = 0; index < 11; index++)
                {
                    Assert.That(closedItemsInRange[index].CycleTime, Is.EqualTo(index + 1));
                }
            }
            ;
        }

        [Test]
        public async Task UpdateTeamMetrics_RefreshesUpdateTimeForTeam()
        {
            testTeam.UpdateTime = DateTime.Now.AddDays(-1);

            await subject.UpdateTeamMetrics(testTeam);

            Assert.That(testTeam.UpdateTime, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-1)));
        }

        [Test]
        public async Task UpdateTeamMetrics_TeamHasAutomaticallyAdjustFeatureWIPSetting_SetsFeatureWIPToRealWIP()
        {
            testTeam.FeatureWIP = 2;
            testTeam.AutomaticallyAdjustFeatureWIP = true;

            AddWorkItem(StateCategories.Doing, 1, "Feature1");
            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            AddWorkItem(StateCategories.Doing, 1, "Feature3");

            await subject.UpdateTeamMetrics(testTeam);

            Assert.That(testTeam.FeatureWIP, Is.EqualTo(3));
        }

        [Test]
        public async Task UpdateTeamMetrics_TeamHasNoAutomaticallyAdjustFeatureWIPSetting_DoesNotChangeWIP()
        {
            testTeam.FeatureWIP = 2;
            testTeam.AutomaticallyAdjustFeatureWIP = false;

            AddWorkItem(StateCategories.Doing, 1, "Feature1");
            AddWorkItem(StateCategories.Doing, 1, "Feature2");
            AddWorkItem(StateCategories.Doing, 1, "Feature3");

            await subject.UpdateTeamMetrics(testTeam);

            Assert.That(testTeam.FeatureWIP, Is.EqualTo(2));
        }

        [Test]
        public async Task UpdateTeamMetrics_TeamHasAutomaticallyAdjustFeatureWIPSetting_NewFeatureWIPIsZero_ChangesFeatureWIP()
        {
            testTeam.FeatureWIP = 2;
            testTeam.AutomaticallyAdjustFeatureWIP = true;

            await subject.UpdateTeamMetrics(testTeam);

            Assert.That(testTeam.FeatureWIP, Is.Zero);
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScoreForTeam_ReturnsScoreBasedOnTeamsThroughputAndHowManyForecast()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);

            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            var howManyForecast = new HowManyForecast();
            var expectedResult = new ForecastPredictabilityScore(howManyForecast);

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 8)).Returns(howManyForecast);

            var score = subject.GetMultiItemForecastPredictabilityScoreForTeam(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(score, Is.Not.Null);
                Assert.That(score.PredictabilityScore, Is.EqualTo(expectedResult.PredictabilityScore));
                foreach (var percentile in score.Percentiles)
                {
                    Assert.That(percentile.Value, Is.EqualTo(expectedResult.Percentiles.Single(p => p.Percentile == percentile.Percentile).Value));
                }
            }
            ;
        }

        [Test]
        public void GetTotalWorkItemAge_NoWorkItemsInDoing_ReturnsZero()
        {
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            Assert.That(totalAge, Is.Zero);
        }

        [Test]
        public void GetTotalWorkItemAge_NoWorkOfTeamInDoing_ReturnsZero()
        {
            AddWorkItem(StateCategories.Doing, 2, string.Empty);

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            Assert.That(totalAge, Is.Zero);
        }

        [Test]
        public void GetTotalWorkItemAge_SingleItemInProgress_ReturnsItemAge()
        {
            var workItem = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            workItem.StartedDate = DateTime.UtcNow.AddDays(-5);

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            Assert.That(totalAge, Is.EqualTo(6));
        }

        [Test]
        public void GetTotalWorkItemAge_MultipleItemsInProgress_ReturnsSumOfAges()
        {
            var workItem1 = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            workItem1.StartedDate = DateTime.UtcNow.AddDays(-5);

            var workItem2 = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            workItem2.StartedDate = DateTime.UtcNow.AddDays(-3);

            var workItem3 = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            workItem3.StartedDate = DateTime.UtcNow.AddDays(-1);

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            // 6 + 4 + 2 = 12
            Assert.That(totalAge, Is.EqualTo(12));
        }

        [Test]
        public void GetTotalWorkItemAge_MixedStateItems_OnlyCountsDoingItems()
        {
            var doingItem = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            doingItem.StartedDate = DateTime.UtcNow.AddDays(-5);

            var doneItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            doneItem.StartedDate = DateTime.UtcNow.AddDays(-10);
            doneItem.ClosedDate = DateTime.UtcNow.AddDays(-2);

            AddWorkItem(StateCategories.ToDo, 1, string.Empty);

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            Assert.That(totalAge, Is.EqualTo(6));
        }

        [Test]
        public void GetTotalWorkItemAge_ItemWithNoStartedDate_UsesCreatedDate()
        {
            var workItem = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            workItem.StartedDate = null;
            workItem.CreatedDate = DateTime.UtcNow.AddDays(-7);

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            Assert.That(totalAge, Is.EqualTo(8));
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_BaselineDatesNotSet_ReturnsBaselineMissing()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineMissing));
                Assert.That(result.DataPoints, Is.Empty);
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_BaselineInvalid_ReturnsBaselineInvalid()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.AddDays(-5);
            testTeam.ProcessBehaviourChartBaselineEndDate = DateTime.UtcNow.AddDays(-2);

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_ReturnsReadyStatus()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-7).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_ReturnsCorrectNumberOfDataPoints()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-7).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            var expectedDays = (displayEnd - displayStart).Days + 1;
            Assert.That(result.DataPoints, Has.Length.EqualTo(expectedDays));
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_ReturnsDateXAxisKind()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-7).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            Assert.That(result.XAxisKind, Is.EqualTo(XAxisKind.Date));
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_XValuesAreFormattedDates()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var displayEnd = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc);

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints[0].XValue, Is.EqualTo("2025-01-01"));
                Assert.That(result.DataPoints[1].XValue, Is.EqualTo("2025-01-02"));
                Assert.That(result.DataPoints[2].XValue, Is.EqualTo("2025-01-03"));
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_YValuesMatchThroughputCounts()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-2).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.ClosedDate = displayStart;

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.ClosedDate = displayStart;

            var item3 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item3.ClosedDate = displayEnd;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints[0].YValue, Is.EqualTo(2));
                Assert.That(result.DataPoints[1].YValue, Is.EqualTo(0));
                Assert.That(result.DataPoints[2].YValue, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_WorkItemIdsIncluded()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-1).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.ClosedDate = displayStart;

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.ClosedDate = displayStart;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints[0].WorkItemIds, Has.Length.EqualTo(2));
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item1.Id));
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item2.Id));
                Assert.That(result.DataPoints[1].WorkItemIds, Is.Empty);
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_ComputesBaselineStatistics()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-3).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            // Add items in baseline period to create known statistics
            for (var day = 0; day < 15; day++)
            {
                var closedDate = baselineStart.AddDays(day);
                var item = AddWorkItem(StateCategories.Done, 1, string.Empty);
                item.ClosedDate = closedDate;
            }

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.Average, Is.GreaterThan(0));
                Assert.That(result.UpperNaturalProcessLimit, Is.GreaterThanOrEqualTo(result.Average));
                Assert.That(result.LowerNaturalProcessLimit, Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_ValidBaseline_LnplClampedToZero()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-1).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            // Sparse throughput in baseline creates conditions where LNPL would be negative
            var item = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item.ClosedDate = baselineStart.AddDays(5);

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            Assert.That(result.LowerNaturalProcessLimit, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_OnlyStartDateSet_ReturnsBaselineInvalid()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = DateTime.UtcNow.AddDays(-30);
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
        }

        private WorkItem AddWorkItem(StateCategories stateCategory, int teamId, string parentReference)
        {
            var workItem = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = stateCategory,
                TeamId = teamId,
                ParentReferenceId = parentReference,
                ClosedDate = DateTime.UtcNow,
                Type = "User Story",
            };

            workItems.Add(workItem);

            return workItem;
        }
    }
}