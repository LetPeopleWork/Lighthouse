using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Distribution;

namespace Lighthouse.Backend.Platform.Releases
{
    public interface ILighthouseReleaseService
    {
        string GetCurrentVersion();

        Task<IEnumerable<LighthouseRelease>> GetNewReleases();

        Task<string?> GetNewestReleaseVersion();

        Task<bool> UpdateAvailable();

        bool IsUpdateSupported();

        Task<bool> InstallUpdate();

        DistributionInfo GetDistributionInfo();
    }
}