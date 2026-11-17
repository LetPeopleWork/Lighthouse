using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth
{
    public class NoOpAuthStrategy : IWorkTrackingAuthStrategy
    {
        public Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
