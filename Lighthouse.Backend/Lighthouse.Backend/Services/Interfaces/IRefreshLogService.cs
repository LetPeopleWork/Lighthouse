using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IRefreshLogService
    {
        Task LogRefreshAsync(RefreshLog entry);

        IEnumerable<RefreshLog> GetRefreshLogs();

        Task RemoveRefreshLogsForEntity(RefreshType type, int entityId);

        Task RemoveOrphanedRefreshLogs();
    }
}
