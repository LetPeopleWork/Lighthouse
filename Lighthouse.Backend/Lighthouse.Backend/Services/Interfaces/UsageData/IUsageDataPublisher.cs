using Lighthouse.Backend.Models.UsageData;

namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// The way out, and the only one: everything else on this path reads what is waiting and hands
    /// it here. Taking a permit rather than a token means the part that sends cannot be reached
    /// without the answer having been checked first.
    /// </summary>
    public interface IUsageDataPublisher
    {
        /// <summary>
        /// Sends one batch. A failure is the caller's to swallow, and there is no second attempt: a
        /// retry is another chance to send something whose consent may have changed in the meantime.
        /// </summary>
        Task PublishAsync(UsageDataEmitPermit permit, AcceptedUsageDataBatch batch, CancellationToken cancellationToken);
    }
}
