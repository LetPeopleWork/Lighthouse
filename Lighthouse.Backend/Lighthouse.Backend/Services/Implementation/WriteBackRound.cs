using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// Everything one refresh round has resolved to write back and to report, and how much of that round
    /// is still to come. A portfolio refresh and the forecast it triggers run as two separate executions
    /// but are one round, and both the work tracking system and the operator should hear from Lighthouse
    /// once per round rather than once per execution.
    /// </summary>
    public sealed class WriteBackRound
    {
        private readonly Dictionary<int, WorkTrackingSystemConnection> connectionsById = [];

        private readonly Dictionary<StagingKey, WriteBackFieldUpdate> stagedUpdates = [];

        private RefreshRoundSummary? refreshSummary;

        private RefreshRoundSummary? forecastSummary;

        private int executionsStillToFinish = 1;

        /// <summary>Whether every execution of this round has finished, so the round can speak for itself.</summary>
        public bool HasFinished => Volatile.Read(ref executionsStillToFinish) == 0;

        /// <summary>
        /// Records that one more execution belongs to this round, so the write waits for it too.
        /// </summary>
        public void Join()
        {
            Interlocked.Increment(ref executionsStillToFinish);
        }

        /// <summary>
        /// Records that an execution of this round has finished. Answers whether it was the last one and
        /// therefore owes the round its write.
        /// </summary>
        public bool Leave()
        {
            return Interlocked.Decrement(ref executionsStillToFinish) == 0;
        }

        /// <summary>What the entity refresh of this round did.</summary>
        public void ReportRefresh(RefreshRoundSummary reported)
        {
            refreshSummary = reported;
        }

        /// <summary>What the forecast of this round did.</summary>
        public void ReportForecast(RefreshRoundSummary reported)
        {
            forecastSummary = reported;
        }

        /// <summary>
        /// The one thing this round has to say, or null when nothing in it got far enough to report.
        /// A forecast folds into the refresh that asked for it as the time it cost; a round that ran
        /// nothing but a forecast has that time as its whole story. Emptying as it hands over is what
        /// keeps a round to a single line even if it is asked twice.
        /// </summary>
        public RefreshRoundSummary? TakeSummary()
        {
            var summary = refreshSummary == null
                ? forecastSummary
                : refreshSummary with
                {
                    ForecastDurationMs = forecastSummary?.ForecastDurationMs,
                    ForecastSucceeded = forecastSummary?.ForecastSucceeded ?? true,
                };

            refreshSummary = null;
            forecastSummary = null;

            return summary;
        }

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

        /// <summary>
        /// Empties the staging area and hands back what was in it, grouped per connection. Draining
        /// before the first write is what makes the write terminal: a second attempt finds nothing rather
        /// than re-sending.
        /// </summary>
        public IReadOnlyList<(WorkTrackingSystemConnection Connection, IReadOnlyList<WriteBackFieldUpdate> Updates)> TakeStaged()
        {
            var pending = stagedUpdates
                .GroupBy(staged => staged.Key.ConnectionId)
                .Select(group => (
                    connectionsById[group.Key],
                    (IReadOnlyList<WriteBackFieldUpdate>)group.Select(staged => staged.Value).ToList()))
                .ToList();

            stagedUpdates.Clear();
            connectionsById.Clear();

            return pending;
        }

        private readonly record struct StagingKey(int ConnectionId, string WorkItemId, string TargetFieldReference);
    }
}
