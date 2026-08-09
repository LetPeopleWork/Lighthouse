using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    // __SCAFFOLD__ - DISTILL RED scaffold for ADR-144 (Epic 5500, slice 01). DELIVER replaces the bodies.
    public class WriteBackCollector(
        IWriteBackService writeBackService,
        ILogger<WriteBackCollector> logger)
        : IWriteBackCollector
    {
        private const string NotImplemented = "Not yet implemented - RED scaffold (ADR-144)";

        public void Stage(WorkTrackingSystemConnection connection, IReadOnlyList<WriteBackFieldUpdate> updates)
        {
            _ = writeBackService;
            _ = logger;
            throw new InvalidOperationException(NotImplemented);
        }

        public Task<IReadOnlyList<WriteBackResult>> FlushAsync()
        {
            throw new InvalidOperationException(NotImplemented);
        }
    }
}
