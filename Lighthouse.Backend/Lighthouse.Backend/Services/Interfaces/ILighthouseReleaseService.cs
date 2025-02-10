using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ILighthouseReleaseService
    {
        string GetCurrentVersion();

        Task<IEnumerable<LighthouseRelease>> GetNewReleases();

        Task<bool> UpdateAvailable();
    }
}