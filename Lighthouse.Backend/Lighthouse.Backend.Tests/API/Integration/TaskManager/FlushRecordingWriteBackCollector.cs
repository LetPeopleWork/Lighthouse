using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// Wraps the real collector rather than replacing it, so the staging and flushing under test is still
    /// the production one and only the fact that a flush happened is recorded on the way through.
    /// </summary>
    public sealed class FlushRecordingWriteBackCollector(IWriteBackCollector inner, FlushCount flushes) : IWriteBackCollector
    {
        public void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
            => inner.Stage(connection, updates);

        public Task<IReadOnlyList<WriteBackResult>> FlushAsync()
        {
            flushes.Increment();
            return inner.FlushAsync();
        }
    }

    /// <summary>
    /// How many executions reported themselves finished to their round. Shared across the scopes one
    /// refresh opens, because the collector is scoped and the round is not.
    /// </summary>
    public sealed class FlushCount
    {
        private int count;

        public int Value => Volatile.Read(ref count);

        public void Increment() => Interlocked.Increment(ref count);

        public void Reset() => Volatile.Write(ref count, 0);
    }
}
