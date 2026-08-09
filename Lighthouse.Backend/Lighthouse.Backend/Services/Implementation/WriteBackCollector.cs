using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WriteBackCollector(
        IWriteBackService writeBackService,
        ILogger<WriteBackCollector> logger)
        : IWriteBackCollector
    {
        private readonly Dictionary<int, WorkTrackingSystemConnection> connectionsById = [];

        private readonly Dictionary<StagingKey, WriteBackFieldUpdate> stagedUpdates = [];

        public void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            foreach (var update in updates)
            {
                // Last stage wins, for the connection as much as for the value: a later pass holds the
                // fresher of both.
                connectionsById[connection.Id] = connection;
                stagedUpdates[new StagingKey(connection.Id, update.WorkItemId, update.TargetFieldReference)] = update;
            }
        }

        public async Task<IReadOnlyList<WriteBackResult>> FlushAsync()
        {
            var pending = TakeStagedUpdates();

            if (pending.Count == 0)
            {
                return [];
            }

            logger.LogDebug("Flushing write-back across {ConnectionCount} connection(s)", pending.Count);

            var results = new List<WriteBackResult>();

            foreach (var (connectionId, updates) in pending)
            {
                results.Add(await writeBackService.WriteFieldsToWorkItems(connectionsById[connectionId], updates));
            }

            connectionsById.Clear();

            return results;
        }

        /// <summary>
        /// Empties the staging area and hands back what was in it, grouped per connection. Draining
        /// before the first write is what makes the flush terminal: a second flush in the same scope
        /// finds nothing rather than re-sending.
        /// </summary>
        private List<(int ConnectionId, IReadOnlyList<WriteBackFieldUpdate> Updates)> TakeStagedUpdates()
        {
            var pending = stagedUpdates
                .GroupBy(staged => staged.Key.ConnectionId)
                .Select(group => (
                    group.Key,
                    (IReadOnlyList<WriteBackFieldUpdate>)group.Select(staged => staged.Value).ToList()))
                .ToList();

            stagedUpdates.Clear();

            return pending;
        }

        private readonly record struct StagingKey(int ConnectionId, string WorkItemId, string TargetFieldReference);
    }
}
