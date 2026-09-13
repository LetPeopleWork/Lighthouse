using Lighthouse.Backend.Services.Interfaces.UsageData;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// Empties what is waiting and makes one call per batch. It reads no table, writes no row and
    /// tells nothing else in the application that anything happened - a boundary rather than a
    /// description, because anything else it could touch is something it could be made to do on a
    /// path nobody is watching.
    ///
    /// The answer is asked again here, on the token the batch carries. Somebody can withdraw in the
    /// moment between handing a batch in and this getting to it, and the record is the only thing
    /// that can say so. What that does not cover, and cannot: a batch already handed to the network
    /// when the withdrawal lands will finish. That is one request in flight, and no design that
    /// sends anywhere closes it.
    /// </summary>
    public sealed class UsageDataForwardingService(
        IUsageDataEventQueue waiting,
        IUsageDataGate gate,
        IEnumerable<IUsageDataPublisher> waysOut,
        ILogger<UsageDataForwardingService> logger) : BackgroundService
    {
        private int couldNotBeSent;

        /// <summary>
        /// Sends everything waiting right now and returns. Kept separate from the loop below so that
        /// whoever needs the queue emptied at a moment they choose - a test above all, since the test
        /// host runs no background work at all - can have exactly that, instead of waiting out a
        /// schedule it cannot see.
        /// </summary>
        public async Task SendWhatIsWaitingAsync(CancellationToken cancellationToken)
        {
            while (waiting.TryTakeNext(out var batch))
            {
                await SendAsync(batch, cancellationToken);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Waiting on the queue rather than on a clock. How long a withdrawal takes to start
            // stopping batches is bounded by how long this sleeps, so there is nothing to be gained
            // by sleeping at all while something is already there.
            while (await waiting.WaitForSomethingAsync(stoppingToken))
            {
                await SendWhatIsWaitingAsync(stoppingToken);
            }
        }

        private async Task SendAsync(AcceptedUsageDataBatch batch, CancellationToken cancellationToken)
        {
            try
            {
                var permit = await gate.RequestPermitToSendAsync(
                    batch.Token, batch.Events.Count, cancellationToken);

                if (permit is null)
                {
                    return;
                }

                foreach (var wayOut in waysOut)
                {
                    await wayOut.PublishAsync(permit, batch, cancellationToken);
                }
            }
            catch (Exception failure)
            {
                // Nothing escapes and nothing is tried again. An exception that ended this loop would
                // stop usage data for as long as the process lives, with nothing failing and nobody
                // told; a second attempt would be a second chance to send something whose consent may
                // have changed since the first.
                couldNotBeSent++;
                logger.LogDebug(
                    failure, "Usage data: {Count} batch(es) could not be sent and were dropped", couldNotBeSent);
            }
        }
    }
}
