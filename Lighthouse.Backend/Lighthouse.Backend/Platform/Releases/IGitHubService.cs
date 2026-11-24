using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Platform.Releases
{
    public interface IGitHubService
    {
        Task<string> GetLatestReleaseVersion();

        Task<IEnumerable<LighthouseRelease>> GetAllReleases();
    }
}
