using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class WriteBackCollector(
        IWriteBackService writeBackService,
        WriteBackRoundContext roundContext,
        ILogger<WriteBackCollector> logger)
        : IWriteBackCollector
    {
        private readonly WriteBackRound round = roundContext.Current ?? new WriteBackRound();

        private bool hasLeftTheRound;

        public void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            round.Stage(connection, updates);
        }

        public async Task<IReadOnlyList<WriteBackResult>> FlushAsync()
        {
            if (hasLeftTheRound)
            {
                return [];
            }

            hasLeftTheRound = true;

            if (!round.Leave())
            {
                // Another execution of this round is still to come - a portfolio refresh that asked for a
                // forecast, for instance. Writing here would reach the work tracking system once for what
                // this execution resolved and again for what that one resolves; the last execution out
                // carries both.
                return [];
            }

            var pending = round.TakeStaged();

            if (pending.Count == 0)
            {
                return [];
            }

            logger.LogDebug("Flushing write-back across {ConnectionCount} connection(s)", pending.Count);

            var results = new List<WriteBackResult>();

            foreach (var (connection, updates) in pending)
            {
                results.Add(await writeBackService.WriteFieldsToWorkItems(connection, updates));
            }

            return results;
        }
    }
}
