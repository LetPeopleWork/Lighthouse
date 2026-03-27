using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class RefreshLogService(
        IRepository<RefreshLog> repository,
        IAppSettingService appSettingService) : IRefreshLogService
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
    }
}
