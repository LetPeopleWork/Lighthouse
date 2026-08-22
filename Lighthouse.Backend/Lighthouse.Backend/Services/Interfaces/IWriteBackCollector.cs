using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Staging seam for write-back intents. Scoped: exactly one instance per update execution, and every
    /// execution of one refresh round stages into the same round, so a refresh reaches the work tracking
    /// system once however many executions it takes.
    /// </summary>
    public interface IWriteBackCollector
    {
        /// <summary>
        /// Stages resolved intents. Deduplicates on (connection, work item, target field); the later
        /// stage wins, because the later pass holds the fresher value. Performs no I/O.
        /// </summary>
        void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);

        /// <summary>
        /// Reports that this execution has finished and, when it is the last of its round, writes
        /// everything the round staged, once per connection, and clears the staging area. Calling it
        /// again in the same execution does nothing.
        /// </summary>
        Task<IReadOnlyList<WriteBackResult>> FlushAsync();
    }
}
