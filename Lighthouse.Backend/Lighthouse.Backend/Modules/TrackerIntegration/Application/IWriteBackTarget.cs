using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    /// <summary>
    /// Writing field values back into the tracker a connection points at. Declared apart from reading, because
    /// only some trackers accept a write; a connector that cannot write does not take this on.
    /// </summary>
    public interface IWriteBackTarget
    {
        Task<WriteBackResult> WriteFieldsToWorkItems(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates);
    }
}
