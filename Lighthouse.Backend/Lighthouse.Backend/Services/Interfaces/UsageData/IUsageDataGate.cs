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
    }
}
