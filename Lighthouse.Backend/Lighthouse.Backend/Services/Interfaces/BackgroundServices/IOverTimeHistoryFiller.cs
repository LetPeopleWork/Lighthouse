using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.BackgroundServices
{
    /// <summary>
    /// One owner's run of days that carry no over-time reading yet, handed over to be worked out
    /// somewhere other than the request that noticed them. The days are candidates rather than
    /// instructions: the pass still drops any that fall past the owner's last observation, which it
    /// can only know once it has loaded the owner.
    ///
    /// The ask names no metric because a pass covers every chart the owner has - percentiles and
    /// process limits alike. Naming one would let the two fill at different moments, which is the
    /// defect this whole seam exists to prevent: a lead comparing the limits chart against the
    /// percentile tabs would be comparing two different stretches of history.
    /// </summary>
    public sealed record OverTimeFillRequest(
        int OwnerId,
        OwnerType OwnerType,
        IReadOnlyList<DateOnly> CandidateDays)
    {
        /// <summary>
        /// What makes two asks the same ask. Opening the same chart three times while the first walk
        /// is still running must not queue the same walk three times.
        ///
        /// The owner and nothing finer, which is only safe because a pass covers all of that owner's
        /// charts. Collapse the key without widening the pass and an ask gets dropped while work it
        /// covers is still outstanding - the family it named would then never be filled at all.
        /// </summary>
        public (int OwnerId, OwnerType OwnerType) Key => (OwnerId, OwnerType);
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
        /// Runs every ask waiting right now, one pass at a time on the caller, and returns once the
        /// queue is empty or cancellation is requested; whatever is still waiting then stays queued.
        /// It does not wait for a pass the background loop has already taken off the queue - that one
        /// finishes on the loop. Separate from the background loop so that whoever needs the queue
        /// emptied at a moment they choose - a test above all, since the test host runs no background
        /// work and so every pass there runs here - can have exactly that instead of sleeping.
        /// </summary>
        Task DrainAsync(CancellationToken cancellationToken);
    }
}
