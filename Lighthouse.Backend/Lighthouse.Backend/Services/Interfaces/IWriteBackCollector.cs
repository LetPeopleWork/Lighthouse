using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Services.Interfaces
{
    /// <summary>
    /// Staging seam for write-back intents (ADR-144). Scoped: exactly one instance per update
    /// execution, so every pass inside one <c>Update</c> stages into the same collector and the
    /// execution reaches the tracker once.
    /// </summary>
    public interface IWriteBackCollector
    {
        /// <summary>
        /// Stages resolved intents. Deduplicates on (connection, work item, target field); the later
        /// stage wins, because the later pass holds the fresher value. Performs no I/O.
        /// </summary>
        void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);

        /// <summary>
        /// Writes everything staged, once per connection, and clears the staging area.
        /// </summary>
        Task<IReadOnlyList<WriteBackResult>> FlushAsync();
    }
}
