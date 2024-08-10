using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class FeatureHistoryServiceTest
    {
        private Mock<IRepository<FeatureHistoryEntry>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<FeatureHistoryEntry>>();
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

        private FeatureHistoryService CreateSubject()
        {
            return new FeatureHistoryService(repositoryMock.Object);
        }
    }
}
