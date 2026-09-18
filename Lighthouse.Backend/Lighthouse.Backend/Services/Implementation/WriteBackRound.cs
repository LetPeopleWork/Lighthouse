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
        /// <summary>
        /// The executions of one round run at the same time as each other, so everything the round holds
        /// on their behalf is reached through here. Every member that takes it is synchronous from end to
        /// end, so nothing is awaited while it is held.
        /// </summary>
        private readonly Lock state = new();

        private readonly Dictionary<int, WorkTrackingSystemConnection> connectionsById = [];

        private readonly Dictionary<StagingKey, WriteBackFieldUpdate> stagedUpdates = [];

        private RefreshRoundSummary? refreshSummary;

        private RefreshRoundSummary? forecastSummary;

        private bool stagingHasBeenHandedOver;

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
            lock (state)
            {
                refreshSummary = reported;
            }
        }

        /// <summary>What the forecast of this round did.</summary>
        public void ReportForecast(RefreshRoundSummary reported)
        {
            lock (state)
            {
                forecastSummary = reported;
            }
        }

        /// <summary>
        /// The one thing this round has to say, or null when nothing in it got far enough to report.
        /// A forecast folds into the refresh that asked for it as the time it cost; a round that ran
        /// nothing but a forecast has that time as its whole story. Reading and emptying without letting
        /// go in between is what keeps a round to a single line: two executions can both find the round
        /// finished and both ask, and only the first of them gets an answer.
        /// </summary>
        public RefreshRoundSummary? TakeSummary()
        {
            lock (state)
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
        }

        /// <summary>
        /// Adds what an execution has resolved to write. Refused once the round has handed its staging
        /// area over, because by then the write has been made and anything arriving late would be held
        /// forever by an object nobody looks at again - a field the operator asked to have written back
        /// that silently never is.
        /// </summary>
        public void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            lock (state)
            {
                if (stagingHasBeenHandedOver)
                {
                    throw new InvalidOperationException(
                        "This refresh round has already handed its updates over to be written. Anything staged now would never reach the work tracking system.");
                }

                foreach (var update in updates)
                {
                    // Last stage wins, for the connection as much as for the value: a later pass holds the
                    // fresher of both.
                    connectionsById[connection.Id] = connection;
                    stagedUpdates[new StagingKey(connection.Id, update.WorkItemId, update.TargetFieldReference)] = update;
                }
            }
        }

        /// <summary>
        /// Empties the staging area and hands back what was in it, grouped per connection. Draining
        /// before the first write is what makes the write terminal: a second attempt finds nothing rather
        /// than re-sending.
        /// </summary>
        public IReadOnlyList<(WorkTrackingSystemConnection Connection, IReadOnlyList<WriteBackFieldUpdate> Updates)> TakeStaged()
        {
            lock (state)
            {
                var pending = stagedUpdates
                    .GroupBy(staged => staged.Key.ConnectionId)
                    .Select(group => (
                        connectionsById[group.Key],
                        (IReadOnlyList<WriteBackFieldUpdate>)group.Select(staged => staged.Value).ToList()))
                    .ToList();

                stagedUpdates.Clear();
                connectionsById.Clear();
                stagingHasBeenHandedOver = true;

                return pending;
            }
        }

        private readonly record struct StagingKey(int ConnectionId, string WorkItemId, string TargetFieldReference);
    }
}
