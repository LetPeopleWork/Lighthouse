using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// The one place an answer to "may this be sent" is actually enforced. The browser decides what
    /// to show; it does not decide what leaves. The endpoint behind this is reachable by anyone who
    /// can reach the instance, so the question is asked again on every batch rather than remembered.
    /// </summary>
    public interface IUsageDataGate
    {
        /// <summary>
        /// Resolves the token a browser presented into permission to forward, or into nothing at all.
        /// Never throws: an absent token, one nobody minted, a refusal, a withdrawal, a browser last
        /// seen too long ago, a database that will not answer and a cancelled request all come back
        /// the same way, because every one of them is a reason not to send.
        /// </summary>
        /// <returns>A permit when this browser is agreeing right now; otherwise <c>null</c>.</returns>
        Task<UsageDataEmitPermit?> RequestPermitAsync(string? token, CancellationToken cancellationToken);

        /// <summary>
        /// The same question, asked where the data actually leaves, and the only one that spends the
        /// day's allowance. Both ends of the pipe ask whether a browser is still agreeing - once when
        /// a batch is taken in and once when it is sent - so a count kept behind the question above
        /// would charge every batch twice and stop sending at half the number an operator configured.
        /// Charging here instead makes the number mean what it says: events that went out.
        /// </summary>
        /// <returns>
        /// A permit when this browser is agreeing right now and the day's allowance covers the whole
        /// batch; otherwise <c>null</c>, which means dropped rather than refused.
        /// </returns>
        Task<UsageDataEmitPermit?> RequestPermitToSendAsync(
            string? token, int events, CancellationToken cancellationToken);

        /// <summary>
        /// Says a batch the allowance was charged for never arrived, so that the allowance stops
        /// being charged for it. Taking it before the send is what keeps two batches from being told
        /// there is room for the same last of it; giving it back is what keeps an unreachable
        /// collector from spending a whole day on nothing and leaving the instance silent until
        /// midnight after the collector comes back.
        ///
        /// Whoever calls this has already decided not to try again, so this is a correction rather
        /// than a deferral - and it is also where an operator finds out that the pipe is broken,
        /// which nothing else on this path can tell them, because sending nothing is how a working
        /// instance behaves too.
        /// </summary>
        void GiveBackWhatCouldNotBeSent(int events, Exception failure);
    }
}
