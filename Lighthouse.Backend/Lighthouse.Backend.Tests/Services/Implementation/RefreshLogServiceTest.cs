using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class RefreshLogServiceTest
    {
        private Mock<IRepository<RefreshLog>> repositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<ILogger<RefreshLogService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<RefreshLog>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            appSettingServiceMock.Setup(x => x.GetRefreshLogRetentionRuns()).Returns(30);
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            loggerMock = new Mock<ILogger<RefreshLogService>>();
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result[0].Id, Is.EqualTo(2));
                Assert.That(result[1].Id, Is.EqualTo(1));
            }
        }

        private RefreshLogService CreateSubject()
        {
            return new RefreshLogService(repositoryMock.Object, appSettingServiceMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, loggerMock.Object);
        }

        [Test]
        public async Task RemoveRefreshLogsForEntity_RemovesMatchingTeamLogs()
        {
            var teamLog1 = CreateEntry(1, RefreshType.Team, 5);
            var teamLog2 = CreateEntry(2, RefreshType.Team, 5);
            var otherLog = CreateEntry(3, RefreshType.Portfolio, 5);

            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns((System.Linq.Expressions.Expression<Func<RefreshLog, bool>> pred) =>
                    new List<RefreshLog> { teamLog1, teamLog2, otherLog }.AsQueryable().Where(pred));

            var service = CreateSubject();

            await service.RemoveRefreshLogsForEntity(RefreshType.Team, 5);

            repositoryMock.Verify(r => r.Remove(teamLog1), Times.Once);
            repositoryMock.Verify(r => r.Remove(teamLog2), Times.Once);
            repositoryMock.Verify(r => r.Remove(otherLog), Times.Never);
            repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task RemoveRefreshLogsForEntity_RemovesMatchingPortfolioLogs()
        {
            var portfolioLog = CreateEntry(1, RefreshType.Portfolio, 7);
            var teamLog = CreateEntry(2, RefreshType.Team, 7);

            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns((System.Linq.Expressions.Expression<Func<RefreshLog, bool>> pred) =>
                    new List<RefreshLog> { portfolioLog, teamLog }.AsQueryable().Where(pred));

            var service = CreateSubject();

            await service.RemoveRefreshLogsForEntity(RefreshType.Portfolio, 7);

            repositoryMock.Verify(r => r.Remove(portfolioLog), Times.Once);
            repositoryMock.Verify(r => r.Remove(teamLog), Times.Never);
            repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task RemoveRefreshLogsForEntity_OverlappingIds_DoesNotCrossDelete()
        {
            var teamLog = CreateEntry(1, RefreshType.Team, 10);
            var portfolioLog = CreateEntry(2, RefreshType.Portfolio, 10);

            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns((System.Linq.Expressions.Expression<Func<RefreshLog, bool>> pred) =>
                    new List<RefreshLog> { teamLog, portfolioLog }.AsQueryable().Where(pred));

            var service = CreateSubject();

            await service.RemoveRefreshLogsForEntity(RefreshType.Team, 10);

            repositoryMock.Verify(r => r.Remove(teamLog), Times.Once);
            repositoryMock.Verify(r => r.Remove(portfolioLog), Times.Never);
        }

        [Test]
        public async Task RemoveRefreshLogsForEntity_NoMatchingLogs_DoesNotSave()
        {
            repositoryMock.Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshLog, bool>>>()))
                .Returns(new List<RefreshLog>().AsQueryable());

            var service = CreateSubject();

            await service.RemoveRefreshLogsForEntity(RefreshType.Team, 99);

            repositoryMock.Verify(r => r.Remove(It.IsAny<RefreshLog>()), Times.Never);
            repositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task RemoveOrphanedRefreshLogs_RemovesLogsForDeletedTeams()
        {
            var existingTeam = new Team { Id = 1, Name = "Existing" };
            teamRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { existingTeam });
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(Array.Empty<Portfolio>());

            var validLog = CreateEntry(1, RefreshType.Team, 1);
            var orphanLog = CreateEntry(2, RefreshType.Team, 99);

            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog> { validLog, orphanLog });

            var service = CreateSubject();

            await service.RemoveOrphanedRefreshLogs();

            repositoryMock.Verify(r => r.Remove(orphanLog), Times.Once);
            repositoryMock.Verify(r => r.Remove(validLog), Times.Never);
            repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task RemoveOrphanedRefreshLogs_RemovesLogsForDeletedPortfolios()
        {
            teamRepositoryMock.Setup(r => r.GetAll()).Returns(Array.Empty<Team>());
            var existingPortfolio = new Portfolio { Id = 2, Name = "Existing" };
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { existingPortfolio });

            var validLog = CreateEntry(1, RefreshType.Portfolio, 2);
            var orphanLog = CreateEntry(2, RefreshType.Portfolio, 88);

            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog> { validLog, orphanLog });

            var service = CreateSubject();

            await service.RemoveOrphanedRefreshLogs();

            repositoryMock.Verify(r => r.Remove(orphanLog), Times.Once);
            repositoryMock.Verify(r => r.Remove(validLog), Times.Never);
            repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task RemoveOrphanedRefreshLogs_OverlappingIds_DoesNotCrossDelete()
        {
            var existingTeam = new Team { Id = 5, Name = "Team5" };
            teamRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { existingTeam });
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(Array.Empty<Portfolio>());

            var teamLog = CreateEntry(1, RefreshType.Team, 5);
            var portfolioLog = CreateEntry(2, RefreshType.Portfolio, 5);

            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog> { teamLog, portfolioLog });

            var service = CreateSubject();

            await service.RemoveOrphanedRefreshLogs();

            repositoryMock.Verify(r => r.Remove(teamLog), Times.Never);
            repositoryMock.Verify(r => r.Remove(portfolioLog), Times.Once);
            repositoryMock.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task RemoveOrphanedRefreshLogs_NoOrphans_DoesNotSave()
        {
            var existingTeam = new Team { Id = 1, Name = "Team1" };
            teamRepositoryMock.Setup(r => r.GetAll()).Returns(new[] { existingTeam });
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(Array.Empty<Portfolio>());

            var validLog = CreateEntry(1, RefreshType.Team, 1);
            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog> { validLog });

            var service = CreateSubject();

            await service.RemoveOrphanedRefreshLogs();

            repositoryMock.Verify(r => r.Remove(It.IsAny<RefreshLog>()), Times.Never);
            repositoryMock.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task RemoveOrphanedRefreshLogs_IsIdempotent()
        {
            teamRepositoryMock.Setup(r => r.GetAll()).Returns(Array.Empty<Team>());
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(Array.Empty<Portfolio>());

            var orphanLog = CreateEntry(1, RefreshType.Team, 1);
            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog> { orphanLog });

            var service = CreateSubject();

            await service.RemoveOrphanedRefreshLogs();

            repositoryMock.Verify(r => r.Remove(orphanLog), Times.Once);

            // Simulate second run where no logs remain
            repositoryMock.Setup(r => r.GetAll()).Returns(new List<RefreshLog>());

            await service.RemoveOrphanedRefreshLogs();

            // Still only one removal total from the first run
            repositoryMock.Verify(r => r.Remove(It.IsAny<RefreshLog>()), Times.Once);
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
