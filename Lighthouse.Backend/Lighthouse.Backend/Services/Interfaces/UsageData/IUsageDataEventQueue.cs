using System.Diagnostics.CodeAnalysis;
using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// Where a batch waits between being taken in and being sent. Nothing here is written down
    /// anywhere: a queue that survived a restart would also survive somebody withdrawing, and would
    /// then send on their behalf afterwards - which is the one thing pressing that button is meant
    /// to prevent. It would also put unsent measurements of a customer's own people into that
    /// customer's database, which is a new kind of thing for whoever runs it to have to reason about.
    /// </summary>
    public interface IUsageDataEventQueue
    {
        /// <summary>
        /// Hands a batch over to wait. Never blocks and never refuses: a browser is waiting on the
        /// request this is called from, and how quickly this instance can reach a third party must
        /// not be something that page can feel. When more is already waiting than may wait, the
        /// batch is dropped, because a measurement that is lost costs nobody anything.
        /// </summary>
        void HandIn(AcceptedUsageDataBatch batch);

        /// <summary>
        /// Takes the next batch that is waiting, if there is one. Returns immediately either way.
        /// </summary>
        bool TryTakeNext([NotNullWhen(true)] out AcceptedUsageDataBatch? batch);

        /// <summary>
        /// Completes once something is waiting, or with <c>false</c> when the caller has been asked
        /// to stop and there is nothing more coming.
        /// </summary>
        ValueTask<bool> WaitForSomethingAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// A batch that was taken in, together with the token presented for it. The token travels with
    /// the batch because the answer is asked a second time when it is sent: somebody can withdraw
    /// while this is still waiting, and only the record can say that they did.
    /// </summary>
    public sealed record AcceptedUsageDataBatch(string Token, IReadOnlyList<UsageDataEventReported> Events);
}
