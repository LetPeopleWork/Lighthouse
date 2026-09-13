using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Lighthouse.Backend.Services.Interfaces.UsageData;

namespace Lighthouse.Backend.Services.Implementation.UsageData
{
    /// <summary>
    /// Memory, and nothing else. A batch lives here from the moment it is taken in until the moment
    /// it is sent or the process ends, whichever comes first.
    /// </summary>
    public sealed class UsageDataEventQueue(ILogger<UsageDataEventQueue> logger) : IUsageDataEventQueue
    {
        /// <summary>
        /// How much may be waiting at once. A browser flushes at most twice a minute and the part
        /// that sends empties this continuously, so reaching this number means sending has stopped
        /// working - and at that point holding on to more of it only trades a measurement nobody
        /// misses for memory a customer's instance needs for its actual job.
        /// </summary>
        public const int MostThatCanWait = 512;

        private readonly Channel<AcceptedUsageDataBatch> waiting =
            // Stryker disable once Initializer,Boolean: these two are hints that let the channel skip
            // work it would otherwise do to stay safe for callers it does not have. Getting either
            // wrong costs speed, not correctness, and there is no arrangement of handing batches in
            // and taking them out that tells the settings apart from the outside. How many may wait
            // is the part that is a rule, and it is checked.
            Channel.CreateBounded<AcceptedUsageDataBatch>(new BoundedChannelOptions(MostThatCanWait)
            {
                SingleReader = true,
                SingleWriter = false,
            });

        public void HandIn(AcceptedUsageDataBatch batch)
        {
            // Offering rather than writing: writing would make the browser's request wait until
            // there was room, which is how one instance being unable to reach a third party turns
            // into every page in the product feeling slow.
            if (!waiting.Writer.TryWrite(batch))
            {
                logger.LogDebug(
                    "Usage data: a batch was dropped because {MostThatCanWait} are already waiting to be sent",
                    MostThatCanWait);
            }
        }

        public bool TryTakeNext([NotNullWhen(true)] out AcceptedUsageDataBatch? batch)
        {
            return waiting.Reader.TryRead(out batch);
        }

        public async ValueTask<bool> WaitForSomethingAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await waiting.Reader.WaitToReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Being asked to stop is not a failure, and whatever is still waiting is meant to be
                // left where it is rather than sent on the way out.
                return false;
            }
        }
    }
}
