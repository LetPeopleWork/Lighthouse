using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class FeatureHistoryServiceTest
    {
        private Mock<IRepository<FeatureHistoryEntry>> repositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<FeatureHistoryEntry>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
        }

        [Test]
        public async Task ArchiveFeature_NoFeatureArchived_AddsNewEntry()
        {
            var feature = new Feature();
            var currentArchive = new List<FeatureHistoryEntry>();

            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<FeatureHistoryEntry, bool>>())).Returns((Func<FeatureHistoryEntry, bool> func) => currentArchive.SingleOrDefault(func));

            var subject = CreateSubject();

            await subject.ArchiveFeature(feature);

            repositoryMock.Verify(x => x.Add(It.IsAny<FeatureHistoryEntry>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task ArchiveFeature_OtherFeaturesArchived_AddsNewEntry()
        {
            var feature = new Feature { Id = 12 };
            var currentArchive = new List<FeatureHistoryEntry> { new FeatureHistoryEntry { FeatureId = 17 } };

            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<FeatureHistoryEntry, bool>>())).Returns((Func<FeatureHistoryEntry, bool> func) => currentArchive.SingleOrDefault(func));

            var subject = CreateSubject();

            await subject.ArchiveFeature(feature);

            repositoryMock.Verify(x => x.Add(It.IsAny<FeatureHistoryEntry>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task ArchiveFeature_SameFeatureOnDifferentDayArchived_AddsNewEntry()
        {
            var feature = new Feature { Id = 12 };
            var currentArchive = new List<FeatureHistoryEntry> { new FeatureHistoryEntry { FeatureId = 12, Snapshot = DateOnly.FromDateTime(DateTime.Today).AddDays(-1) } };

            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<FeatureHistoryEntry, bool>>())).Returns((Func<FeatureHistoryEntry, bool> func) => currentArchive.SingleOrDefault(func));

            var subject = CreateSubject();

            await subject.ArchiveFeature(feature);

            repositoryMock.Verify(x => x.Add(It.IsAny<FeatureHistoryEntry>()));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task ArchiveFeature_SameFeatureAlreadyArchivedToday_Updates()
        {
            var feature = new Feature { Id = 12 };
            var featureHistoryEntry = new FeatureHistoryEntry { FeatureId = 12, Snapshot = DateOnly.FromDateTime(DateTime.Today) };
            var currentArchive = new List<FeatureHistoryEntry> { featureHistoryEntry };

            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<FeatureHistoryEntry, bool>>())).Returns((Func<FeatureHistoryEntry, bool> func) => currentArchive.SingleOrDefault(func));

            var subject = CreateSubject();

            await subject.ArchiveFeature(feature);

            repositoryMock.Verify(x => x.Update(featureHistoryEntry));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        [TestCase(10, 20, false)]
        [TestCase(10, 11, false)]
        [TestCase(10, 10, false)]
        [TestCase(10, 9, true)]
        public async Task CleanupData_TakesAgeFromAppSettings_RemovesItemsOlderThanSetting(int featureAge, int removeOlderThanDays, bool shouldRemove)
        {
            var feature = new Feature { Id = 12 };
            var featureHistoryEntry = new FeatureHistoryEntry { Id = 42, FeatureId = 12, Snapshot = DateOnly.FromDateTime(DateTime.Today.AddDays(-featureAge)) };

            var currentArchive = new List<FeatureHistoryEntry> { featureHistoryEntry };
            repositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<FeatureHistoryEntry, bool>>>()))
                .Returns((Expression<Func<FeatureHistoryEntry, bool>> predicate) => currentArchive.Where(predicate.Compile()).AsQueryable());

            appSettingServiceMock.Setup(x => x.GetCleanUpDataHistorySettings()).Returns(new CleanUpDataHistorySettings { MaxStorageTimeInDays = removeOlderThanDays });

            var subject = CreateSubject();
            await subject.CleanupData();

            var times = shouldRemove ? Times.Once() : Times.Never();
            repositoryMock.Verify(x => x.Remove(42), times);
            repositoryMock.Verify(x => x.Save(), Times.Once);
        }

        private FeatureHistoryService CreateSubject()
        {
            return new FeatureHistoryService(repositoryMock.Object, appSettingServiceMock.Object, Mock.Of<ILogger<FeatureHistoryService>>());
        }
    }
}
