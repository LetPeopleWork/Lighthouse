using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth
{
    public class NoOpAuthStrategy : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
