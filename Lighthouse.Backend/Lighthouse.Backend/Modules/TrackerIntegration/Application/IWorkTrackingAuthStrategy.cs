using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IWorkTrackingAuthStrategy
    {
        Task ApplyAsync(HttpRequestMessage request, WorkTrackingSystemConnection connection, CancellationToken cancellationToken);
    }
}
