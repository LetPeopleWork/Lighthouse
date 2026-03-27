using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class RefreshLogServiceTest
    {
        private Mock<IRepository<RefreshLog>> repositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<RefreshLog>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            appSettingServiceMock.Setup(x => x.GetRefreshLogRetentionRuns()).Returns(30);
        }

        [Test]
        public async Task LogRefreshAsync_AddsEntryAndSaves()
        {
            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns(new List<RefreshLog>().AsQueryable());

            var service = CreateSubject();
            var entry = CreateEntry(1, RefreshType.Team, 1);

            await service.LogRefreshAsync(entry);

            repositoryMock.Verify(r => r.Add(entry), Times.Once);
            repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task LogRefreshAsync_PrunesToRetentionLimit()
        {
            const int retention = 3;
            appSettingServiceMock.Setup(x => x.GetRefreshLogRetentionRuns()).Returns(retention);

            var existing = Enumerable.Range(1, retention + 2)
                .Select(i => CreateEntry(i, RefreshType.Team, 1, DateTime.UtcNow.AddMinutes(-i)))
                .ToList();

            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns(existing.AsQueryable());

            var service = CreateSubject();
            var newEntry = CreateEntry(0, RefreshType.Team, 1);

            await service.LogRefreshAsync(newEntry);

            repositoryMock.Verify(r => r.Remove(It.IsAny<RefreshLog>()), Times.Exactly(2));
        }

        [Test]
        public async Task LogRefreshAsync_NoExcess_DoesNotRemoveAnything()
        {
            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns(new List<RefreshLog>().AsQueryable());

            var service = CreateSubject();
            var entry = CreateEntry(1, RefreshType.Team, 1);

            await service.LogRefreshAsync(entry);

            repositoryMock.Verify(r => r.Remove(It.IsAny<RefreshLog>()), Times.Never);
        }

        [Test]
        public async Task LogRefreshAsync_DoesNotPruneEntriesForDifferentEntity()
        {
            const int retention = 2;
            appSettingServiceMock.Setup(x => x.GetRefreshLogRetentionRuns()).Returns(retention);

            var existingForOtherEntity = Enumerable.Range(1, 5)
                .Select(i => CreateEntry(i, RefreshType.Team, entityId: 99, DateTime.UtcNow.AddMinutes(-i)))
                .ToList();

            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns(new List<RefreshLog>().AsQueryable());

            var service = CreateSubject();
            var entry = CreateEntry(0, RefreshType.Team, entityId: 1);

            await service.LogRefreshAsync(entry);

            repositoryMock.Verify(r => r.Remove(It.IsAny<RefreshLog>()), Times.Never);
        }

        [Test]
        public void GetRefreshLogs_ReturnsLogsOrderedByExecutedAtDescending()
        {
            var older = CreateEntry(1, RefreshType.Team, 1, DateTime.UtcNow.AddMinutes(-60));
            var newer = CreateEntry(2, RefreshType.Portfolio, 2, DateTime.UtcNow.AddMinutes(-5));
            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog> { older, newer });

            var service = CreateSubject();

            var result = service.GetRefreshLogs().ToList();

            Assert.That(result[0].Id, Is.EqualTo(2));
            Assert.That(result[1].Id, Is.EqualTo(1));
        }

        private RefreshLogService CreateSubject()
        {
            return new RefreshLogService(repositoryMock.Object, appSettingServiceMock.Object);
        }

        private static RefreshLog CreateEntry(int id, RefreshType type, int entityId, DateTime? executedAt = null)
        {
            return new RefreshLog
            {
                Id = id,
                Type = type,
                EntityId = entityId,
                EntityName = $"Entity{entityId}",
                ItemCount = 5,
                DurationMs = 100,
                ExecutedAt = executedAt ?? DateTime.UtcNow,
                Success = true
            };
        }
    }
}
