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
        private Mock<IRepository<BlackoutPeriod>> blackoutPeriodRepositoryMock;

        private Team testTeam;

        private TeamMetricsService subject;
        private List<WorkItem> workItems;

        [SetUp]
        public void Setup()
        {
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            forecastServiceMock = new Mock<IForecastService>();
            blackoutPeriodRepositoryMock = new Mock<IRepository<BlackoutPeriod>>();
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll()).Returns(Enumerable.Empty<BlackoutPeriod>().AsQueryable());

            var appSettingsServiceMock = new Mock<IAppSettingService>();
            appSettingsServiceMock.Setup(x => x.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(forecastServiceMock.Object);

            testTeam = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30 };
            subject = new TeamMetricsService(Mock.Of<ILogger<TeamMetricsService>>(), workItemRepositoryMock.Object, featureRepositoryMock.Object, appSettingsServiceMock.Object, serviceProvider.Object, blackoutPeriodRepositoryMock.Object);

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
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-1);

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

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            Assert.That(throughput.Total, Is.Zero);
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemForOtherTeamDone_ReturnsEmpty()
        {
            AddWorkItem(StateCategories.Done, 2, string.Empty);

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

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

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.Total, Is.EqualTo(10));
                Assert.That(throughput.History, Is.EqualTo(10));

                foreach (var kvp in throughput.WorkItemsPerUnitOfTime)
                {
                    Assert.That(kvp.Value, Has.Count.EqualTo(1));
                }
            }
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemClosedBeforeHistory_DoesNotIncludeInThroughput()
        {
            testTeam.ThroughputHistory = 10;

            // After
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(+1);

            // In Range
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-5);

            // Before
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-11);

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(1));
        }

        [Test]
        public void GetCurrentThroughputForTeam_ItemClosedAfterHistory_DoesNotIncludeInThroughput()
        {
            testTeam.ThroughputHistory = 10;
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-2);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(11);

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

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

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

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
        }

        [Test]
        public void GetCurrentThroughputForTeam_CachesValue()
        {
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-1);

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);
            Assert.That(throughput.Total, Is.EqualTo(1));

            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-1);
            throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            Assert.That(throughput.Total, Is.EqualTo(1));
        }

        [Test]
        public void GetCurrentThroughputForTeam_InvalidateCache()
        {
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-1);

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);
            Assert.That(throughput.Total, Is.EqualTo(1));

            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-1);
            subject.InvalidateTeamMetrics(testTeam);

            throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

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
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-11);

            var wipData = subject.GetWorkInProgressOverTimeForTeam(testTeam, startDate, endDate);

            Assert.That(wipData.Total, Is.Zero);
        }

        [Test]
        public void GetWorkInProgressOverTime_NoWorkOfTeamInDoing_ReturnsEmpty()
        {
            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            AddWorkItem(StateCategories.Doing, 2, string.Empty);

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

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), 30)).Returns(howManyForecast);

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
        }

        [Test]
        public void GetTotalWorkItemAge_NoWorkItemsInDoing_ReturnsZero()
        {
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);

            // Item was closed yesterday
            var closedItem = AddWorkItem(StateCategories.Done, 1, string.Empty);
            closedItem.ClosedDate = DateTime.UtcNow.AddDays(-1);

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
            workItem.ClosedDate = null;

            var totalAge = subject.GetTotalWorkItemAge(testTeam);

            Assert.That(totalAge, Is.EqualTo(8));
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetThroughputProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetThroughputProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(31));
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
                Assert.That(result.DataPoints[1].YValue, Is.Zero);
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

        // --- WIP PBC Tests ---

        [Test]
        public void GetWipProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetWipProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetWipProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetWipProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(31));
            }
        }

        [Test]
        public void GetWipProcessBehaviourChart_ValidBaseline_ReturnsReadyWithDataPoints()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-3).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            // Add an in-progress item
            var item = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            item.StartedDate = displayStart.AddDays(-1);
            item.ClosedDate = null;

            var result = subject.GetWipProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.XAxisKind, Is.EqualTo(XAxisKind.Date));
                Assert.That(result.DataPoints, Has.Length.EqualTo(4));
            }
        }

        [Test]
        public void GetWipProcessBehaviourChart_ValidBaseline_YValuesMatchWipCounts()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-2).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            // Item in progress for entire display range
            var item1 = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            item1.StartedDate = displayStart.AddDays(-1);
            item1.ClosedDate = null;

            // Item closed during display range
            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = displayStart.AddDays(-1);
            item2.ClosedDate = displayStart.AddDays(1);

            var result = subject.GetWipProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints[0].YValue, Is.EqualTo(2)); // Both in progress on day 0
                Assert.That(result.DataPoints[1].YValue, Is.EqualTo(1)); // item2 closed on day 1
                Assert.That(result.DataPoints[2].YValue, Is.EqualTo(1)); // Only item1
            }
        }

        [Test]
        public void GetWipProcessBehaviourChart_ValidBaseline_LnplClampedToZero()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-1).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetWipProcessBehaviourChart(testTeam, displayStart, displayEnd);

            Assert.That(result.LowerNaturalProcessLimit, Is.GreaterThanOrEqualTo(0));
        }

        // --- Total Work Item Age PBC Tests ---

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(31));
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_ValidBaseline_ReturnsReadyWithDataPoints()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-3).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.XAxisKind, Is.EqualTo(XAxisKind.Date));
                Assert.That(result.DataPoints, Has.Length.EqualTo(4));
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_ValidBaseline_AgeIncreasesOverTime()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-2).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            item.StartedDate = displayStart.AddDays(-5);
            item.ClosedDate = null;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                // Age should increase each day
                Assert.That(result.DataPoints[1].YValue, Is.GreaterThan(result.DataPoints[0].YValue));
                Assert.That(result.DataPoints[2].YValue, Is.GreaterThan(result.DataPoints[1].YValue));
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_ValidBaseline_WorkItemIdsIncluded()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-1).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item = AddWorkItem(StateCategories.Doing, 1, string.Empty);
            item.StartedDate = displayStart.AddDays(-1);
            item.ClosedDate = null;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item.Id));
            }
        }

        [Test]
        public void GetTotalWorkItemAgeProcessBehaviourChart_ValidBaseline_LnplClampedToZero()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-1).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var result = subject.GetTotalWorkItemAgeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            Assert.That(result.LowerNaturalProcessLimit, Is.GreaterThanOrEqualTo(0));
        }

        // --- Cycle Time PBC Tests ---

        [Test]
        public void GetCycleTimeProcessBehaviourChart_BaselineDatesNotSet_ShortRange_ReturnsBaselineInvalid()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var result = subject.GetCycleTimeProcessBehaviourChart(testTeam, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.BaselineConfigured, Is.False);
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_BaselineDatesNotSet_LongRange_ReturnsReadyWithImplicitBaseline()
        {
            testTeam.ProcessBehaviourChartBaselineStartDate = null;
            testTeam.ProcessBehaviourChartBaselineEndDate = null;

            var displayStart = DateTime.UtcNow.AddDays(-30).Date;
            var displayEnd = DateTime.UtcNow.Date;

            var item = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item.StartedDate = displayStart;
            item.ClosedDate = displayStart.AddDays(3);

            var result = subject.GetCycleTimeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.BaselineConfigured, Is.False);
                Assert.That(result.DataPoints, Has.Length.EqualTo(1));
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_ValidBaseline_ReturnsReadyWithDateTimeXAxis()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-7).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item.StartedDate = displayStart;
            item.ClosedDate = displayStart.AddDays(3);

            var result = subject.GetCycleTimeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(result.XAxisKind, Is.EqualTo(XAxisKind.DateTime));
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_ValidBaseline_PerItemDataPoints()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-7).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = displayStart;
            item1.ClosedDate = displayStart.AddDays(2);

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = displayStart;
            item2.ClosedDate = displayStart.AddDays(5);

            var result = subject.GetCycleTimeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints, Has.Length.EqualTo(2));
                Assert.That(result.DataPoints[0].YValue, Is.EqualTo(item1.CycleTime));
                Assert.That(result.DataPoints[1].YValue, Is.EqualTo(item2.CycleTime));
                Assert.That(result.DataPoints[0].WorkItemIds, Is.EqualTo(new[] { item1.Id }));
                Assert.That(result.DataPoints[1].WorkItemIds, Is.EqualTo(new[] { item2.Id }));
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_ValidBaseline_OrderedByClosedDateThenId()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = DateTime.UtcNow.AddDays(-7).Date;
            var displayEnd = DateTime.UtcNow.Date;

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            // item2 created first (lower ID), closed later
            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = displayStart;
            item1.ClosedDate = displayStart.AddDays(5);

            // item2 created second (higher ID), closed earlier
            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = displayStart;
            item2.ClosedDate = displayStart.AddDays(2);

            var result = subject.GetCycleTimeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            using (Assert.EnterMultipleScope())
            {
                // Should be ordered by ClosedDate: item2 first, then item1
                Assert.That(result.DataPoints[0].WorkItemIds[0], Is.EqualTo(item2.Id));
                Assert.That(result.DataPoints[1].WorkItemIds[0], Is.EqualTo(item1.Id));
            }
        }

        [Test]
        public void GetCycleTimeProcessBehaviourChart_ValidBaseline_XValuesAreIsoDateTimes()
        {
            var baselineStart = DateTime.UtcNow.AddDays(-30).Date;
            var baselineEnd = DateTime.UtcNow.AddDays(-16).Date;
            var displayStart = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var displayEnd = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc);

            testTeam.ProcessBehaviourChartBaselineStartDate = baselineStart;
            testTeam.ProcessBehaviourChartBaselineEndDate = baselineEnd;

            var item = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item.StartedDate = displayStart;
            item.ClosedDate = new DateTime(2025, 6, 5, 14, 30, 0, DateTimeKind.Utc);

            var result = subject.GetCycleTimeProcessBehaviourChart(testTeam, displayStart, displayEnd);

            Assert.That(result.DataPoints[0].XValue, Is.EqualTo("2025-06-05T14:30:00Z"));
        }

        #region GetForecastInputCandidates

        [Test]
        public void GetForecastInputCandidates_OnlyDoingItems_CurrentWipCountEqualsDoingCount()
        {
            AddWorkItem(StateCategories.Doing, 1, string.Empty);
            AddWorkItem(StateCategories.Doing, 1, string.Empty);
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(Enumerable.Empty<Feature>().AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            Assert.That(result.CurrentWipCount, Is.EqualTo(2));
        }

        [Test]
        public void GetForecastInputCandidates_DoingAndToDoItems_BacklogCountEqualsSum()
        {
            AddWorkItem(StateCategories.Doing, 1, string.Empty);
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.ToDo, 1, string.Empty);
            AddWorkItem(StateCategories.Done, 1, string.Empty);

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(Enumerable.Empty<Feature>().AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            Assert.That(result.BacklogCount, Is.EqualTo(3));
        }

        [Test]
        public void GetForecastInputCandidates_ItemsFromOtherTeam_ExcludedFromCounts()
        {
            AddWorkItem(StateCategories.Doing, 1, string.Empty);
            AddWorkItem(StateCategories.Doing, 2, string.Empty);
            AddWorkItem(StateCategories.ToDo, 2, string.Empty);

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(Enumerable.Empty<Feature>().AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.CurrentWipCount, Is.EqualTo(1));
                Assert.That(result.BacklogCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetForecastInputCandidates_NoItems_ReturnsBothCountsZero()
        {
            featureRepositoryMock.Setup(r => r.GetAll()).Returns(Enumerable.Empty<Feature>().AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.CurrentWipCount, Is.Zero);
                Assert.That(result.BacklogCount, Is.Zero);
            }
        }

        [Test]
        public void GetForecastInputCandidates_FeatureWithRemainingWorkForTeam_IncludesInFeatureList()
        {
            var feature = new Feature { Id = 1, Name = "Feature A" };
            feature.FeatureWork.Add(new FeatureWork(testTeam, 5, 10, feature));

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { feature }.AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Features, Has.Count.EqualTo(1));
                Assert.That(result.Features[0].Id, Is.EqualTo(1));
                Assert.That(result.Features[0].Name, Is.EqualTo("Feature A"));
                Assert.That(result.Features[0].RemainingWork, Is.EqualTo(5));
            }
        }

        [Test]
        public void GetForecastInputCandidates_FeatureWithZeroRemainingWorkForTeam_ExcludedFromFeatureList()
        {
            var feature = new Feature { Id = 1, Name = "Feature Done" };
            feature.FeatureWork.Add(new FeatureWork(testTeam, 0, 10, feature));

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { feature }.AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            Assert.That(result.Features, Is.Empty);
        }

        [Test]
        public void GetForecastInputCandidates_FeatureWithRemainingWorkForDifferentTeam_ExcludedFromFeatureList()
        {
            var otherTeam = new Team { Id = 99, Name = "Other Team" };
            var feature = new Feature { Id = 1, Name = "Feature B" };
            feature.FeatureWork.Add(new FeatureWork(otherTeam, 8, 10, feature));

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { feature }.AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            Assert.That(result.Features, Is.Empty);
        }

        [Test]
        public void GetForecastInputCandidates_MultipleFeatures_OnlyIncludesFeaturesWithRemainingWork()
        {
            var featureA = new Feature { Id = 1, Name = "Feature A" };
            featureA.FeatureWork.Add(new FeatureWork(testTeam, 3, 10, featureA));

            var featureB = new Feature { Id = 2, Name = "Feature B" };
            featureB.FeatureWork.Add(new FeatureWork(testTeam, 0, 5, featureB));

            var featureC = new Feature { Id = 3, Name = "Feature C" };
            featureC.FeatureWork.Add(new FeatureWork(testTeam, 7, 12, featureC));

            featureRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { featureA, featureB, featureC }.AsQueryable());

            var result = subject.GetForecastInputCandidates(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Features, Has.Count.EqualTo(2));
                Assert.That(result.Features.Select(f => f.Id), Is.EquivalentTo([1, 3]));
            }
        }

        #endregion

        private WorkItem AddWorkItem(StateCategories stateCategory, int teamId, string parentReference)
        {
            var workItem = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = stateCategory,
                TeamId = teamId,
                ParentReferenceId = parentReference,
                Type = "User Story",
            };

            if (stateCategory == StateCategories.Done)
            {
                workItem.ClosedDate = DateTime.UtcNow.AddDays(1);
            }

            if (stateCategory != StateCategories.ToDo)
            {
                workItem.StartedDate = DateTime.UtcNow.AddDays(-5);
            }

            workItems.Add(workItem);

            return workItem;
        }

        #region GetEstimationVsCycleTimeData

        [Test]
        public void GetEstimationVsCycleTimeData_NoEstimationFieldConfigured_ReturnsNotConfigured()
        {
            testTeam.EstimationAdditionalFieldDefinitionId = null;

            var result = subject.GetEstimationVsCycleTimeData(testTeam, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NotConfigured));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.Diagnostics.TotalCount, Is.Zero);
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_EstimationConfiguredButNoClosedItems_ReturnsNoData()
        {
            testTeam.EstimationAdditionalFieldDefinitionId = 42;

            var result = subject.GetEstimationVsCycleTimeData(testTeam, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NoData));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.Diagnostics.TotalCount, Is.Zero);
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_NumericEstimates_ReturnsMappedDataPoints()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(10);
            item2.AdditionalFieldValues[fieldId] = "8";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.DataPoints, Has.Count.EqualTo(2));
                Assert.That(result.Diagnostics.TotalCount, Is.EqualTo(2));
                Assert.That(result.Diagnostics.MappedCount, Is.EqualTo(2));
                Assert.That(result.Diagnostics.UnmappedCount, Is.Zero);
                Assert.That(result.Diagnostics.InvalidCount, Is.Zero);
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_NumericEstimates_DataPointsHaveCorrectValues()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                var dataPoint = result.DataPoints[0];
                Assert.That(dataPoint.EstimationNumericValue, Is.EqualTo(3.0));
                Assert.That(dataPoint.EstimationDisplayValue, Is.EqualTo("3"));
                Assert.That(dataPoint.CycleTime, Is.EqualTo(item1.CycleTime));
                Assert.That(dataPoint.WorkItemIds, Does.Contain(item1.Id));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_DecimalEstimates_PreservesDecimals()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3.5";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            Assert.That(result.DataPoints[0].EstimationNumericValue, Is.EqualTo(3.5));
        }

        [Test]
        public void GetEstimationVsCycleTimeData_SameEstimateAndCycleTime_GroupsIntoSingleDataPoint()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(5);
            item2.AdditionalFieldValues[fieldId] = "3";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints, Has.Count.EqualTo(1));
                Assert.That(result.DataPoints[0].WorkItemIds, Has.Length.EqualTo(2));
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item1.Id));
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item2.Id));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_InvalidEstimate_ExcludedFromDataPoints()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(5);
            item2.AdditionalFieldValues[fieldId] = "not-a-number";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.DataPoints, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics.MappedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics.InvalidCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_MissingEstimate_ExcludedFromDataPoints()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "5";

            // item2 has no estimate value at all
            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(3);

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics.MappedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics.InvalidCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_AllInvalid_ReturnsNoData()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = false;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "abc";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.NoData));
                Assert.That(result.DataPoints, Is.Empty);
                Assert.That(result.Diagnostics.TotalCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics.InvalidCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_NonNumericMode_MapsCategoriesToOrdinalPositions()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = true;
            testTeam.EstimationCategoryValues = ["XS", "S", "M", "L", "XL"];

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(3);
            item1.AdditionalFieldValues[fieldId] = "M";

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(7);
            item2.AdditionalFieldValues[fieldId] = "XL";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            var expectedCategories = new[] { "XS", "S", "M", "L", "XL" };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status, Is.EqualTo(EstimationVsCycleTimeStatus.Ready));
                Assert.That(result.UseNonNumericEstimation, Is.True);
                Assert.That(result.CategoryValues, Is.EqualTo(expectedCategories));
                Assert.That(result.DataPoints, Has.Count.EqualTo(2));

                var mDataPoint = result.DataPoints.First(dp => dp.EstimationDisplayValue == "M");
                Assert.That(mDataPoint.EstimationNumericValue, Is.EqualTo(2));

                var xlDataPoint = result.DataPoints.First(dp => dp.EstimationDisplayValue == "XL");
                Assert.That(xlDataPoint.EstimationNumericValue, Is.EqualTo(4));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_NonNumericMode_UnmappedValuesExcludedFromDataPoints()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.UseNonNumericEstimation = true;
            testTeam.EstimationCategoryValues = ["S", "M", "L"];

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(3);
            item1.AdditionalFieldValues[fieldId] = "M";

            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(5);
            item2.AdditionalFieldValues[fieldId] = "XXL";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics.MappedCount, Is.EqualTo(1));
                Assert.That(result.Diagnostics.UnmappedCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_ReturnsEstimationUnit()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;
            testTeam.EstimationUnit = "Points";

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            Assert.That(result.EstimationUnit, Is.EqualTo("Points"));
        }

        [Test]
        public void GetEstimationVsCycleTimeData_OnlyIncludesItemsFromTeam()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-30).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            // Different team
            var item2 = AddWorkItem(StateCategories.Done, 2, string.Empty);
            item2.StartedDate = startDate;
            item2.ClosedDate = startDate.AddDays(5);
            item2.AdditionalFieldValues[fieldId] = "5";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints, Has.Count.EqualTo(1));
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item1.Id));
            }
        }

        [Test]
        public void GetEstimationVsCycleTimeData_OnlyIncludesItemsInDateRange()
        {
            const int fieldId = 42;
            testTeam.EstimationAdditionalFieldDefinitionId = fieldId;

            var startDate = DateTime.UtcNow.AddDays(-10).Date;
            var endDate = DateTime.UtcNow.Date;

            var item1 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item1.StartedDate = startDate;
            item1.ClosedDate = startDate.AddDays(5);
            item1.AdditionalFieldValues[fieldId] = "3";

            // Closed before start date
            var item2 = AddWorkItem(StateCategories.Done, 1, string.Empty);
            item2.StartedDate = startDate.AddDays(-20);
            item2.ClosedDate = startDate.AddDays(-15);
            item2.AdditionalFieldValues[fieldId] = "5";

            var result = subject.GetEstimationVsCycleTimeData(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DataPoints, Has.Count.EqualTo(1));
                Assert.That(result.DataPoints[0].WorkItemIds, Does.Contain(item1.Id));
            }
        }

        #endregion

        #region Blackout Period Filtering

        [Test]
        public void GetCurrentThroughputForTeam_RollingWindow_NoBlackoutPeriods_BehaviorUnchanged()
        {
            testTeam.ThroughputHistory = 10;

            for (var i = 0; i < 10; i++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-i);
            }

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.Total, Is.EqualTo(10));
                Assert.That(throughput.History, Is.EqualTo(10));
            }
        }

        [Test]
        public void GetCurrentThroughputForTeam_RollingWindow_WithBlackoutDays_ExcludesBlackoutDays()
        {
            testTeam.ThroughputHistory = 10;

            for (var i = 0; i < 12; i++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-i);
            }

            // Blackout 2 days ago and 3 days ago
            var blackoutStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-3));
            var blackoutEnd = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(new[] { new BlackoutPeriod { Id = 1, Start = blackoutStart, End = blackoutEnd } }.AsQueryable());

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.History, Is.EqualTo(10));
            }
        }

        [Test]
        public void GetCurrentThroughputForTeam_RollingWindow_WithBlackoutDays_ThroughputExcludesBlackoutDayItems()
        {
            testTeam.ThroughputHistory = 5;

            // Day 0 (today): 1 item
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow;
            // Day -1: 1 item
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-1);
            // Day -2: 1 item (BLACKOUT)
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-2);
            // Day -3: 1 item
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-3);
            // Day -4: 1 item
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-4);
            // Day -5: 1 item (extended window)
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-5);

            var blackoutDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(new[] { new BlackoutPeriod { Id = 1, Start = blackoutDate, End = blackoutDate } }.AsQueryable());

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            using (Assert.EnterMultipleScope())
            {
                // 5 effective days (day -2 excluded, window extended to include day -5)
                Assert.That(throughput.History, Is.EqualTo(5));
                // 5 items from the 5 non-blackout days
                Assert.That(throughput.Total, Is.EqualTo(5));
            }
        }

        [Test]
        public void GetCurrentThroughputForTeam_FixedWindow_WithBlackoutDays_ReducesSamples()
        {
            testTeam.UseFixedDatesForThroughput = true;
            testTeam.ThroughputHistoryStartDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            testTeam.ThroughputHistoryEndDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            // Add items on specific dates
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 2, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 3, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            // Blackout Apr 4-6 (3 days)
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(new[] { new BlackoutPeriod { Id = 1, Start = new DateOnly(1991, 4, 4), End = new DateOnly(1991, 4, 6) } }.AsQueryable());

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            using (Assert.EnterMultipleScope())
            {
                // 10 calendar days - 3 blackout days = 7 effective days
                Assert.That(throughput.History, Is.EqualTo(7));
                // Item on Apr 5 is excluded (inside blackout), items on Apr 1,2,3,8,10 remain
                Assert.That(throughput.Total, Is.EqualTo(5));
            }
        }

        [Test]
        public void GetBlackoutAwareThroughputForTeam_ExcludesBlackoutDays()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 2, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            // Blackout Apr 4-6
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(new[] { new BlackoutPeriod { Id = 1, Start = new DateOnly(1991, 4, 4), End = new DateOnly(1991, 4, 6) } }.AsQueryable());

            var throughput = subject.GetBlackoutAwareThroughputForTeam(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                // 10 days - 3 blackout days = 7 effective days
                Assert.That(throughput.History, Is.EqualTo(7));
                // Item on Apr 5 excluded
                Assert.That(throughput.Total, Is.EqualTo(3));
            }
        }

        [Test]
        public void GetBlackoutAwareThroughputForTeam_NoBlackoutPeriods_ReturnsSameAsGetThroughputForTeam()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 5, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            var throughput = subject.GetBlackoutAwareThroughputForTeam(testTeam, startDate, endDate);
            var normalThroughput = subject.GetThroughputForTeam(testTeam, startDate, endDate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.History, Is.EqualTo(normalThroughput.History));
                Assert.That(throughput.Total, Is.EqualTo(normalThroughput.Total));
            }
        }

        [Test]
        public void GetCurrentThroughputForTeam_RollingWindow_MultipleBlackoutPeriods_ExcludesAll()
        {
            testTeam.ThroughputHistory = 5;

            for (var i = 0; i < 10; i++)
            {
                AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = DateTime.UtcNow.AddDays(-i);
            }

            // Two separate blackout periods
            var blackout1Start = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
            var blackout1End = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-2));
            var blackout2Start = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-5));
            var blackout2End = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-5));
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(new[]
                {
                    new BlackoutPeriod { Id = 1, Start = blackout1Start, End = blackout1End },
                    new BlackoutPeriod { Id = 2, Start = blackout2Start, End = blackout2End }
                }.AsQueryable());

            var throughput = subject.GetCurrentThroughputForTeamForecast(testTeam);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(throughput.History, Is.EqualTo(5));
            }
        }

        [Test]
        public void GetMultiItemForecastPredictabilityScoreForTeam_WithBlackoutDays_UsesBlackoutAwareThroughput()
        {
            var startDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 2, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 8, 0, 0, 0, DateTimeKind.Utc);
            AddWorkItem(StateCategories.Done, 1, string.Empty).ClosedDate = new DateTime(1991, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            // Blackout Apr 4-6 (3 days)
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll())
                .Returns(new[] { new BlackoutPeriod { Id = 1, Start = new DateOnly(1991, 4, 4), End = new DateOnly(1991, 4, 6) } }.AsQueryable());

            forecastServiceMock.Setup(x => x.HowMany(It.IsAny<RunChartData>(), It.IsAny<int>()))
                .Returns((RunChartData throughput, int days) =>
                {
                    // Verify the throughput passed to HowMany has blackout days excluded
                    Assert.That(throughput.History, Is.EqualTo(7));
                    return new HowManyForecast();
                });

            subject.GetMultiItemForecastPredictabilityScoreForTeam(testTeam, startDate, endDate);

            forecastServiceMock.Verify(x => x.HowMany(It.Is<RunChartData>(t => t.History == 7), It.IsAny<int>()), Times.Once);
        }

        #endregion
    }
}