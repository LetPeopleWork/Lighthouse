using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.BackgroundServices
{
    /// <summary>
    /// One owner's run of days that carry no over-time reading yet, handed over to be worked out
    /// somewhere other than the request that noticed them. The days are candidates rather than
    /// instructions: the pass still drops any that fall past the owner's last observation, which it
    /// can only know once it has loaded the owner.
    /// </summary>
    public sealed record OverTimeFillRequest(
        int OwnerId,
        OwnerType OwnerType,
        MetricType MetricType,
        IReadOnlyList<DateOnly> CandidateDays)
    {
        /// <summary>
        /// What makes two asks the same ask. Opening the same chart three times while the first walk
        /// is still running must not queue the same walk three times.
        /// </summary>
        public (int OwnerId, OwnerType OwnerType, MetricType MetricType) Key
            => (OwnerId, OwnerType, MetricType);
    }

    /// <summary>
    /// Whether history is being filled in at this instant, and nothing else. The database maintenance
    /// gate needs that one fact before it lets an operator swap the database file out from under an
    /// open write, and must not be able to reach past it into starting, stopping or queueing a fill.
    /// </summary>
    public interface IOverTimeHistoryFillActivity
    {
        bool HasPassInFlight { get; }
    }

    public interface IOverTimeHistoryFiller : IOverTimeHistoryFillActivity
    {
        /// <summary>
        /// Takes the ask and returns immediately. Never throws and never blocks: the caller is a read
        /// a person is waiting on, and nothing about filling in history is worth making them wait for.
        /// </summary>
        void AskFor(OverTimeFillRequest request);

        /// <summary>
        /// Works through everything waiting right now and returns once the queue is empty and no pass
        /// is still writing. Separate from the background loop so that whoever needs the queue emptied
        /// at a moment they choose - a test above all, since the test host runs no background work -
        /// can have exactly that instead of sleeping.
        /// </summary>
        Task DrainAsync(CancellationToken cancellationToken);
    }
}
