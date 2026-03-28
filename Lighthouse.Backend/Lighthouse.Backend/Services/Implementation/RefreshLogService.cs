using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Services.Implementation
{
    public class RefreshLogService(
        IRepository<RefreshLog> repository,
        IAppSettingService appSettingService,
        IRepository<Team> teamRepository,
        IRepository<Portfolio> portfolioRepository,
        ILogger<RefreshLogService> logger) : IRefreshLogService
    {
        public async Task LogRefreshAsync(RefreshLog entry)
        {
            repository.Add(entry);
            await repository.Save();

            var retentionCount = appSettingService.GetRefreshLogRetentionRuns();

            var excess = repository
                .GetAllByPredicate(r => r.EntityId == entry.EntityId && r.Type == entry.Type)
                .OrderByDescending(r => r.ExecutedAt)
                .Skip(retentionCount)
                .ToList();

            foreach (var old in excess)
            {
                repository.Remove(old);
            }

            if (excess.Count > 0)
            {
                await repository.Save();
            }
        }

        public IEnumerable<RefreshLog> GetRefreshLogs()
        {
            return repository.GetAll().OrderByDescending(r => r.ExecutedAt);
        }

        public async Task RemoveRefreshLogsForEntity(RefreshType type, int entityId)
        {
            var logsToRemove = repository
                .GetAllByPredicate(r => r.Type == type && r.EntityId == entityId)
                .ToList();

            if (logsToRemove.Count == 0)
            {
                logger.LogDebug("No refresh logs found for {EntityType} {EntityId}", type, entityId);
                return;
            }

            foreach (var log in logsToRemove)
            {
                repository.Remove(log);
            }

            await repository.Save();

            logger.LogInformation("Removed {Count} refresh logs for {EntityType} {EntityId}", logsToRemove.Count, type, entityId);
        }

        public async Task RemoveOrphanedRefreshLogs()
        {
            var allLogs = repository.GetAll().ToList();
            var existingTeamIds = new HashSet<int>(teamRepository.GetAll().Select(t => t.Id));
            var existingPortfolioIds = new HashSet<int>(portfolioRepository.GetAll().Select(p => p.Id));

            var orphans = allLogs.Where(log => log.Type switch
            {
                RefreshType.Team => !existingTeamIds.Contains(log.EntityId),
                RefreshType.Portfolio => !existingPortfolioIds.Contains(log.EntityId),
                _ => false
            }).ToList();

            logger.LogDebug("Orphan reconciliation scanned {Total} refresh logs, found {OrphanCount} orphans", allLogs.Count, orphans.Count);

            if (orphans.Count == 0)
            {
                return;
            }

            foreach (var orphan in orphans)
            {
                repository.Remove(orphan);
            }

            await repository.Save();

            logger.LogInformation("Startup reconciliation removed {Count} orphaned refresh logs", orphans.Count);
        }
    }
}
