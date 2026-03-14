using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Distribution;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ILighthouseReleaseService
    {
        string GetCurrentVersion();

        Task<IEnumerable<LighthouseRelease>> GetNewReleases();

        Task<bool> UpdateAvailable();

        bool IsUpdateSupported();

        Task<bool> InstallUpdate();

        DistributionInfo GetDistributionInfo();
    }
}