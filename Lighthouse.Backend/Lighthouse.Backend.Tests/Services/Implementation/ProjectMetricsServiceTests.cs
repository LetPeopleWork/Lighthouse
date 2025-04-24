using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class ProjectMetricsServiceTests
    {
        private Mock<ILogger<ProjectMetricsService>> logger;
        private Mock<IRepository<Feature>> featureRepository;
        private Mock<IAppSettingService> appSettingService;
        private ProjectMetricsService sut;
        private Project project;
        private List<Feature> features;

        [SetUp]
        public void Setup()
        {
            logger = new Mock<ILogger<ProjectMetricsService>>();
            featureRepository = new Mock<IRepository<Feature>>();
            appSettingService = new Mock<IAppSettingService>();

            appSettingService.Setup(m => m.GetFeaturRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });
            
            sut = new ProjectMetricsService(logger.Object, featureRepository.Object, appSettingService.Object);
            
            SetupTestData();
        }

        [Test]
        public void GetThroughputForProject_ReturnsRunChartDataWithCorrectValues()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 10);
            
            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();
            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(closedFeatures);

            // Act
            var result = sut.GetThroughputForProject(project, startDate, endDate);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.ValuePerUnitOfTime, Has.Length.EqualTo(10));
                Assert.That(result.ValuePerUnitOfTime.Sum(), Is.EqualTo(2));
            });
        }

        [Test]
        public void GetFeaturesInProgressOverTimeForProject_ReturnsCorrectRunChartData()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 5);
            
            var activeFeatures = features.Where(f => f.StateCategory == StateCategories.Doing || f.StateCategory == StateCategories.Done).AsQueryable();
            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(activeFeatures);

            // Act
            var result = sut.GetFeaturesInProgressOverTimeForProject(project, startDate, endDate);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.ValuePerUnitOfTime, Has.Length.EqualTo(5));
            });
        }

        [Test]
        public void GetInProgressFeaturesForProject_ReturnsActiveFeatures()
        {
            // Arrange
            var activeFeatures = features.Where(f => f.StateCategory == StateCategories.Doing).AsQueryable();
            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(activeFeatures);

            // Act
            var result = sut.GetInProgressFeaturesForProject(project).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.First().ReferenceId, Is.EqualTo("F3"));
            });
        }

        [Test]
        public void GetCycleTimePercentilesForProject_ReturnsCorrectPercentileValues()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            
            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();
            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(closedFeatures);

            var result = sut.GetCycleTimePercentilesForProject(project, startDate, endDate).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(4));
                Assert.That(result[0].Percentile, Is.EqualTo(50));
                Assert.That(result[1].Percentile, Is.EqualTo(70));
                Assert.That(result[2].Percentile, Is.EqualTo(85));
                Assert.That(result[3].Percentile, Is.EqualTo(95));
            });
        }

        [Test]
        public void GetCycleTimePercentilesForProject_NoClosedItems_ReturnsEmpty()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            
            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(new List<Feature>().AsQueryable());

            // Act
            var result = sut.GetCycleTimePercentilesForProject(project, startDate, endDate).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void GetCycleTimeDataForProject_ReturnsClosedFeatures()
        {
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            
            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(closedFeatures);

            var result = sut.GetCycleTimeDataForProject(project, startDate, endDate).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.True);
            });
        }

        [Test]
        public void GetCycleTimeDataForProject_FeatureClosedAtEndDate_ReturnsFeature()
        {
            var startDate = DateTime.UtcNow.AddDays(-1);
            var endDate = DateTime.UtcNow;

            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            closedFeatures.First().ClosedDate = DateTime.Now;

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(closedFeatures);

            var result = sut.GetCycleTimeDataForProject(project, startDate, endDate).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.False);
            });
        }

        [Test]
        public void GetCycleTimeDataForProject_FeatureClosedAtStartDate_ReturnsFeature()
        {
            var startDate = DateTime.Today.AddDays(-1);
            var endDate = DateTime.Today;

            var closedFeatures = features.Where(f => f.StateCategory == StateCategories.Done).AsQueryable();

            closedFeatures.First().ClosedDate = DateTime.Now.AddDays(-1);

            featureRepository.Setup(x => x.GetAllByPredicate(
                It.IsAny<Expression<Func<Feature, bool>>>()))
                .Returns(closedFeatures);

            var result = sut.GetCycleTimeDataForProject(project, startDate, endDate).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.Any(f => f.ReferenceId == "F1"), Is.True);
                Assert.That(result.Any(f => f.ReferenceId == "F2"), Is.False);
            });
        }

        [Test]
        public void InvalidateProjectMetrics_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => sut.InvalidateProjectMetrics(project));
        }

        private void SetupTestData()
        {
            project = new Project
            {
                Id = 1,
                Name = "Test Project"
            };

            var jan2 = new DateTime(2023, 1, 2);
            var jan4 = new DateTime(2023, 1, 4);
            var jan5 = new DateTime(2023, 1, 5);
            var jan10 = new DateTime(2023, 1, 10);

            features = new List<Feature>
            {
                new Feature
                {
                    Id = 1,
                    Name = "Feature 1",
                    ReferenceId = "F1",
                    StartedDate = jan2,
                    ClosedDate = jan5,
                    StateCategory = StateCategories.Done,
                },
                new Feature
                {
                    Id = 2,
                    Name = "Feature 2",
                    ReferenceId = "F2",
                    StartedDate = jan4,
                    ClosedDate = jan10,
                    StateCategory = StateCategories.Done,
                },
                new Feature
                {
                    Id = 3,
                    Name = "Feature 3",
                    ReferenceId = "F3",
                    StartedDate = jan2,
                    StateCategory = StateCategories.Doing,
                }
            };

            features.ForEach(f => f.Projects.Add(project));
        }
    }
}