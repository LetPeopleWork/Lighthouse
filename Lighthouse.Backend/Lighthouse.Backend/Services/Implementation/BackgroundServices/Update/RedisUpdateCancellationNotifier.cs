using Lighthouse.Backend.Services.Interfaces.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// RED scaffold (DISTILL, slice 04). Carries "stop this" to whichever replica is running it.
    /// </summary>
    public sealed class RedisUpdateCancellationNotifier : IUpdateCancellationNotifier
    {
        public RedisUpdateCancellationNotifier(IConnectionMultiplexer multiplexer)
        {
            ArgumentNullException.ThrowIfNull(multiplexer);
        }

        public Task PublishCancellationAsync(UpdateKey key)
        {
            throw new NotImplementedException("Not yet implemented - RED scaffold");
        }

        public IDisposable Subscribe(Action<UpdateKey> onCancelled)
        {
            throw new NotImplementedException("Not yet implemented - RED scaffold");
        }
    }
}
